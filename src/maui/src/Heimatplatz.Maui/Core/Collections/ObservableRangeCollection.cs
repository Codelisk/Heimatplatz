using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Heimatplatz.Maui.Core.Collections;

/// <summary>
/// ObservableCollection mit Batch-Ersetzen: <see cref="ReplaceRange"/> tauscht den
/// kompletten Inhalt mit einer einzigen Reset-Notification aus. Die CollectionView
/// behaelt dadurch ihre ItemsSource-Instanz und ihren Container-Recycling-Pool -
/// eine neue Collection-Instanz (oder Clear + N einzelne Adds) wuerde alle
/// realisierten Item-Container verwerfen und komplett neu aufbauen.
/// </summary>
public class ObservableRangeCollection<T> : ObservableCollection<T>
{
    public void ReplaceRange(IEnumerable<T> items)
    {
        CheckReentrancy();

        Items.Clear();
        foreach (var item in items)
            Items.Add(item);

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
