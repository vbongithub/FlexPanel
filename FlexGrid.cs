using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Vbongithub
{
    public class FlexGrid : Grid
    {
        #region Properties

        public IEnumerable<RowDefinition> RowDef
        {
            get { return GetValue (RowDefProperty) as IEnumerable<RowDefinition>; }
            set { SetValue (RowDefProperty, value); }
        }

        public static readonly DependencyProperty RowDefProperty
            = DependencyProperty.Register (
                "RowDef",
                typeof (IEnumerable<RowDefinition>),
                typeof (FlexGrid),
                new FrameworkPropertyMetadata (
                    null,
                    FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                    OnRowDefChanged
                    )
                );

        public IEnumerable<ColumnDefinition> ColDef
        {
            get { return GetValue (ColDefProperty) as IEnumerable<ColumnDefinition>; }
            set { SetValue (ColDefProperty, value); }
        }

        public static readonly DependencyProperty ColDefProperty
            = DependencyProperty.Register (
                "ColDef",
                typeof (IEnumerable<ColumnDefinition>),
                typeof (FlexGrid),
                new FrameworkPropertyMetadata (
                    null,
                    FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                    OnColumnDefChanged
                    )
                );

        #endregion Properties

        #region Util

        private static void OnColumnDefChanged ( DependencyObject src, DependencyPropertyChangedEventArgs e )
        {
            if (src is FlexGrid)
            {
                var srcGrid = (src as FlexGrid);

                var oldVal = e.OldValue as IEnumerable<ColumnDefinition>;
                if (oldVal != null)
                {
                    srcGrid.RemoveColumnDef (oldVal);

                    if (oldVal is INotifyCollectionChanged)
                    {
                        (oldVal as INotifyCollectionChanged).CollectionChanged
                            -= srcGrid.OnColDefCollectionChanged;
                    }
                }

                var val = e.NewValue as IEnumerable<ColumnDefinition>;
                if (val != null)
                {
                    srcGrid.AddColumnDef (val);

                    if (val is INotifyCollectionChanged)
                    {
                        (val as INotifyCollectionChanged).CollectionChanged
                            += srcGrid.OnColDefCollectionChanged;
                    }
                }
            }
        }

        private static void OnRowDefChanged ( DependencyObject src, DependencyPropertyChangedEventArgs e )
        {
            if (src is FlexGrid)
            {
                var srcGrid = (src as FlexGrid);

                var oldVal = e.OldValue as IEnumerable<RowDefinition>;
                if (oldVal != null)
                {
                    srcGrid.RemoveRowDef (oldVal);

                    if (oldVal is INotifyCollectionChanged)
                    {
                        (oldVal as INotifyCollectionChanged).CollectionChanged
                            -= srcGrid.OnRowDefCollectionChanged;
                    }
                }

                var val = e.NewValue as IEnumerable<RowDefinition>;
                if (val != null)
                {
                    srcGrid.AddRowDef (val);

                    if (val is INotifyCollectionChanged)
                    {
                        (val as INotifyCollectionChanged).CollectionChanged
                            += srcGrid.OnRowDefCollectionChanged;
                    }
                }
            }
        }

        private void OnRowDefCollectionChanged ( object sender, NotifyCollectionChangedEventArgs e )
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                AddRowDef (
                    e.NewItems.Cast<RowDefinition> ()
                    );
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                RemoveRowDef (
                    e.OldItems.Cast<RowDefinition> ()
                    );
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                RowDefinitions.Clear ();
            }
        }

        private void AddRowDef ( IEnumerable<RowDefinition> rowDefs )
        {
            rowDefs.ToList ()
                .ForEach (RowDefinitions.Add);
        }

        private void RemoveRowDef ( IEnumerable<RowDefinition> rowDefs )
        {
            rowDefs.ToList ()
                .ForEach (func => RowDefinitions.Remove (func));
        }

        private void OnColDefCollectionChanged ( object sender, NotifyCollectionChangedEventArgs e )
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                AddColumnDef (
                    e.NewItems.Cast<ColumnDefinition> ()
                    );
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                RemoveColumnDef (
                    e.OldItems.Cast<ColumnDefinition> ()
                    );
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                ColumnDefinitions.Clear ();
            }
        }

        private void AddColumnDef ( IEnumerable<ColumnDefinition> ColumnDefs )
        {
            ColumnDefs.ToList ()
                .ForEach (ColumnDefinitions.Add);
        }

        private void RemoveColumnDef ( IEnumerable<ColumnDefinition> ColumnDefs )
        {
            ColumnDefs.ToList ()
                .ForEach (func => ColumnDefinitions.Remove (func));
        }

        #endregion Util
    }
}
