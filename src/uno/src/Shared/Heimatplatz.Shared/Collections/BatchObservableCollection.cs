using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Heimatplatz.Collections;

/// <summary>
/// An ObservableCollection that supports batch-replacing all items with a single
/// <see cref="NotifyCollectionChangedAction.Reset"/> notification instead of
/// firing individual Add/Remove events for each item.
///
/// This prevents StackOverflow in Uno Platform's ItemsRepeater / binding pipeline
/// where N individual CollectionChanged events cascade through DependencyProperty
/// invalidation and layout cycles.
/// </summary>
public class BatchObservableCollection<T> : ObservableCollection<T>
{
    /// <summary>
    /// Replaces all items in the collection atomically, firing a single
    /// <see cref="NotifyCollectionChangedAction.Reset"/> notification.
    /// </summary>
    /// <param name="items">The new set of items. Null is treated as empty.</param>
    public void Reset(IEnumerable<T>? items)
    {
        CheckReentrancy();

        // Manipulate the underlying List<T> directly — no per-item notifications.
        Items.Clear();

        if (items is not null)
        {
            foreach (var item in items)
            {
                Items.Add(item);
            }
        }

        // Single notification for the entire batch.
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
    }
}
