using System.Collections.Specialized;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

namespace Heimatplatz.Collections;

/// <summary>
/// An ObservableCollection that supports ISupportIncrementalLoading for automatic
/// pagination when used with ListView or ItemsRepeater with SupportsIncrementalLoading.
///
/// Extends BatchObservableCollection to prevent StackOverflow issues in Uno Platform.
/// </summary>
/// <typeparam name="T">The type of items in the collection</typeparam>
public class PaginatedObservableCollection<T> : BatchObservableCollection<T>, ISupportIncrementalLoading
{
    private readonly Func<int, int, CancellationToken, Task<(IEnumerable<T> Items, bool HasMore)>> _loadMoreAsync;
    private int _currentPage;
    private bool _hasMore = true;
    private bool _isLoading;

    /// <summary>
    /// Gets whether there are more items to load.
    /// Returns false if currently loading or no more items available.
    /// </summary>
    public bool HasMoreItems => _hasMore && !_isLoading;

    /// <summary>
    /// Gets the page size used for pagination.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Gets the total number of items currently loaded.
    /// </summary>
    public int TotalLoaded => Count;

    /// <summary>
    /// Gets whether a load operation is currently in progress.
    /// </summary>
    public bool IsLoading => _isLoading;

    /// <summary>
    /// Event raised when IsLoading changes.
    /// </summary>
    public event EventHandler? IsLoadingChanged;

    /// <summary>
    /// Creates a new PaginatedObservableCollection.
    /// </summary>
    /// <param name="loadMoreAsync">
    /// Async function to load more items.
    /// Parameters: (page, pageSize, cancellationToken)
    /// Returns: (items, hasMore)
    /// </param>
    /// <param name="pageSize">Number of items to load per page. Default is 20.</param>
    public PaginatedObservableCollection(
        Func<int, int, CancellationToken, Task<(IEnumerable<T> Items, bool HasMore)>> loadMoreAsync,
        int pageSize = 20)
    {
        _loadMoreAsync = loadMoreAsync ?? throw new ArgumentNullException(nameof(loadMoreAsync));
        PageSize = pageSize;
    }

    /// <summary>
    /// Loads more items asynchronously. Called automatically by ListView/ItemsRepeater
    /// when scrolling approaches the end of the list.
    /// </summary>
    public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
    {
        return new LoadMoreItemsAsyncOperation(this, count);
    }

    /// <summary>
    /// Internal method to perform the actual load operation.
    /// </summary>
    internal async Task<LoadMoreItemsResult> LoadMoreItemsInternalAsync(CancellationToken ct)
    {
        if (_isLoading || !_hasMore)
            return new LoadMoreItemsResult { Count = 0 };

        SetIsLoading(true);
        try
        {
            var (items, hasMore) = await _loadMoreAsync(_currentPage, PageSize, ct);
            var itemsList = items.ToList();

            // Add items using direct manipulation (no per-item notifications)
            foreach (var item in itemsList)
            {
                Items.Add(item);
            }

            // Single notification for the batch
            if (itemsList.Count > 0)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Count)));
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            }

            _hasMore = hasMore;
            _currentPage++;

            return new LoadMoreItemsResult { Count = (uint)itemsList.Count };
        }
        catch (OperationCanceledException)
        {
            return new LoadMoreItemsResult { Count = 0 };
        }
        finally
        {
            SetIsLoading(false);
        }
    }

    /// <summary>
    /// Resets the collection and reloads the first page.
    /// Call this when filters change or when a refresh is needed.
    /// </summary>
    public async Task ResetAndReloadAsync(CancellationToken ct = default)
    {
        // Clear existing items
        Items.Clear();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));

        // Reset pagination state
        _currentPage = 0;
        _hasMore = true;

        // Load first page
        await LoadMoreItemsInternalAsync(ct);
    }

    /// <summary>
    /// Refreshes the collection by clearing and reloading from the beginning.
    /// </summary>
    public Task RefreshAsync(CancellationToken ct = default) => ResetAndReloadAsync(ct);

    private void SetIsLoading(bool value)
    {
        if (_isLoading != value)
        {
            _isLoading = value;
            IsLoadingChanged?.Invoke(this, EventArgs.Empty);
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(IsLoading)));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(HasMoreItems)));
        }
    }

    /// <summary>
    /// Custom IAsyncOperation implementation for Uno Platform compatibility.
    /// </summary>
    private sealed class LoadMoreItemsAsyncOperation : IAsyncOperation<LoadMoreItemsResult>
    {
        private readonly PaginatedObservableCollection<T> _collection;
        private readonly CancellationTokenSource _cts = new();
        private AsyncOperationCompletedHandler<LoadMoreItemsResult>? _completed;
        private LoadMoreItemsResult _result;
        private Exception? _exception;
        private AsyncStatus _status = AsyncStatus.Started;

        public LoadMoreItemsAsyncOperation(PaginatedObservableCollection<T> collection, uint count)
        {
            _collection = collection;
            StartAsync();
        }

        private async void StartAsync()
        {
            try
            {
                _result = await _collection.LoadMoreItemsInternalAsync(_cts.Token);
                _status = AsyncStatus.Completed;
            }
            catch (OperationCanceledException)
            {
                _status = AsyncStatus.Canceled;
            }
            catch (Exception ex)
            {
                _exception = ex;
                _status = AsyncStatus.Error;
            }

            _completed?.Invoke(this, _status);
        }

        public AsyncOperationCompletedHandler<LoadMoreItemsResult>? Completed
        {
            get => _completed;
            set
            {
                _completed = value;
                if (_status != AsyncStatus.Started)
                {
                    value?.Invoke(this, _status);
                }
            }
        }

        public Exception? ErrorCode => _exception;
        public uint Id => 0;
        public AsyncStatus Status => _status;

        public void Cancel() => _cts.Cancel();
        public void Close() => _cts.Dispose();

        public LoadMoreItemsResult GetResults()
        {
            if (_status == AsyncStatus.Error && _exception != null)
                throw _exception;
            return _result;
        }
    }
}
