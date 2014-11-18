using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Vbongithub
{
    public class FlexPanel : ListBox
    {
        public ObservableCollection<RowDefinition> RowDefinitions { get; set; }

        public ObservableCollection<ColumnDefinition> ColumnDefinitions { get; set; }

        public FlexPanel ( )
        {
            RowDefinitions
                = new ObservableCollection<RowDefinition> ();
            ColumnDefinitions
                = new ObservableCollection<ColumnDefinition> ();

            SetValue (
                ScrollViewer.HorizontalScrollBarVisibilityProperty,
                ScrollBarVisibility.Disabled
                );
            SetValue (
                ScrollViewer.VerticalScrollBarVisibilityProperty,
                ScrollBarVisibility.Disabled
                );

            Loaded += ( src, e ) =>
            {
                SizeChanged
                    += OnSizeChanged;

                LoadLayout ();
            };

            Unloaded += ( src, e ) =>
            {
                SizeChanged
                    -= OnSizeChanged;

                SaveLayout ();
            };
        }

        private void LoadLayout ( )
        {
            Properties.Settings.Default.Reload ();

            if (string.IsNullOrWhiteSpace (Properties.Settings.Default.GridLayoutSetting)) return;

            Predicate<double> isValid
                    = ( data ) => !(double.IsNaN (data) || double.IsInfinity (data));

            var settings
                = Properties.Settings.Default.GridLayoutSetting
                    .Split (new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select (LayoutSetting.Parse)
                    .ToList ();

            settings.Where (func => func.IsRow ())
                .Where (func => isValid (func.Size))
                .Join (
                    RowDefinitions.Select (( Row, Pos ) => new { Row, Pos }),
                    k => k.Pos,
                    k => k.Pos,
                    ( l, r ) => new { l, r }
                    )
                .ToList ()
                .ForEach (func =>
                    func.r.Row.Height = new GridLength (func.l.Size, GridUnitType.Star)
                    );

            settings.Where (func => func.IsCol ())
                .Where (func => isValid (func.Size))
                .Join (
                    ColumnDefinitions.Select (( Col, Pos ) => new { Col, Pos }),
                    k => k.Pos,
                    k => k.Pos,
                    ( l, r ) => new { l, r }
                    )
                .ToList ()
                .ForEach (func =>
                    func.r.Col.Width = new GridLength (func.l.Size, GridUnitType.Star)
                    );
        }

        private void SaveLayout ( )
        {
            if (string.IsNullOrWhiteSpace (Properties.Settings.Default.GridLayoutSetting)) return;

            var layoutSetting
                = new StringBuilder ();

            if (RowDefinitions != null)
            {
                var totalHeight
                   = RowDefinitions
                       .Select (func => func.ActualHeight)
                       .Sum ();

                layoutSetting.Append (
                    string.Join (
                        "|",
                        RowDefinitions.Select (
                            ( func, pos ) =>
                                string.Format ("R:{0}:{1}", pos, Math.Round (func.ActualHeight / totalHeight, 4))
                            )
                        )
                    );
            }

            layoutSetting.Append ("|");

            if (ColumnDefinitions != null)
            {
                var totalWidth
                    = ColumnDefinitions
                        .Select (func => func.ActualWidth)
                        .Sum ();

                layoutSetting.Append (
                    string.Join (
                        "|",
                        ColumnDefinitions.Select (
                            ( func, pos ) =>
                                string.Format ("C:{0}:{1}", pos, Math.Round (func.ActualWidth / totalWidth, 4))
                            )
                        )
                    );
            }

            Properties.Settings.Default.GridLayoutSetting
                = layoutSetting.ToString ();

            Properties.Settings.Default.Save ();
        }

        private void OnSizeChanged ( object sender, SizeChangedEventArgs e )
        {
            if (RowDefinitions != null)
            {
                var totalHeight
                    = RowDefinitions
                        .Select (func => func.ActualHeight)
                        .Sum ();

                RowDefinitions
                    .Select (func => new
                    {
                        RowDef = func,
                        NewHeight = new GridLength (func.ActualHeight / totalHeight, GridUnitType.Star)
                    })
                    .ToList ()
                    .ForEach (func => func.RowDef.Height = func.NewHeight);
            }

            if (ColumnDefinitions != null)
            {
                var totalWidth
                    = ColumnDefinitions
                        .Select (func => func.ActualWidth)
                        .Sum ();

                ColumnDefinitions
                    .Select (func => new
                    {
                        ColDef = func,
                        NewWidth = new GridLength (func.ActualWidth / totalWidth, GridUnitType.Star)
                    })
                    .ToList ()
                    .ForEach (func => func.ColDef.Width = func.NewWidth);
            }

            SaveLayout ();
        }

        public void OnHorizontalDragDelta ( ListBoxItem sender, DragDeltaEventArgs e )
        {
            var colQuery
                = getColDefs (
                    sender.GetValue (Grid.ColumnProperty) as int?,
                    sender.GetValue (Grid.ColumnSpanProperty) as int?
                    );

            Action<ColumnDefinition, double> setWidth
                = ( colDef, width ) =>
                    {
                        if (width >= 0)
                        {
                            colDef.Width
                                = new GridLength (
                                    width
                                    );
                        }
                    };

            var leftCol
                = colQuery.FirstOrDefault ();
            if (leftCol != null)
            {
                setWidth (
                    leftCol,
                    leftCol.ActualWidth + e.HorizontalChange
                    );
            }

            var rightCol
                = colQuery.Skip (1).FirstOrDefault ();
            if (rightCol != null)
            {
                setWidth (
                     rightCol,
                     rightCol.ActualWidth - e.HorizontalChange
                     );
            }
            
            SaveLayout ( );
        }

        public void OnVerticalDragDelta ( ListBoxItem sender, DragDeltaEventArgs e )
        {
            var rowQuery
                = getRowDefs (
                    sender.GetValue (Grid.RowProperty) as int?,
                    sender.GetValue (Grid.RowSpanProperty) as int?
                    );

            Action<RowDefinition, double> setHeight
               = ( rowDef, height ) =>
               {
                   if (height >= 0)
                   {
                       rowDef.Height
                           = new GridLength (
                               height
                               );
                   }
               };

            var topRow
                = rowQuery.FirstOrDefault ();
            if (topRow != null)
            {
                setHeight (
                    topRow,
                    topRow.ActualHeight + e.VerticalChange
                    );
            }

            var bottomRow
                = rowQuery.Skip (1).FirstOrDefault ();
            if (bottomRow != null)
            {
                setHeight (
                    bottomRow,
                    bottomRow.ActualHeight - e.VerticalChange
                    );
            }
            
            SaveLayout ( );
        }

        public void OnCornerDragDelta ( ListBoxItem sender, DragDeltaEventArgs e )
        {
            OnHorizontalDragDelta (sender, e);
            OnVerticalDragDelta (sender, e);
        }

        private IEnumerable<ColumnDefinition> getColDefs ( int? colIndex, int? colSpan )
        {
            var tempList = new List<ColumnDefinition> ();

            if (ColumnDefinitions != null && colIndex.HasValue && colSpan.HasValue)
            {
                tempList.AddRange (
                    ColumnDefinitions
                        .Skip (colIndex.Value + colSpan.Value - 1)
                        .Take (2)
                    );
            }

            return tempList;
        }

        private IEnumerable<RowDefinition> getRowDefs ( int? rowIndex, int? rowSpan )
        {
            var tempList = new List<RowDefinition> ();

            if (RowDefinitions != null && rowIndex.HasValue && rowSpan.HasValue)
            {
                tempList.AddRange (
                    RowDefinitions
                        .Skip (rowIndex.Value + rowSpan.Value - 1)
                        .Take (2)
                    );
            }

            return tempList;
        }

        private class LayoutSetting
        {
            public string Attrb { get; private set; }

            public int Pos { get; private set; }

            public double Size { get; private set; }

            private LayoutSetting ( )
            {
            }

            public static LayoutSetting Parse ( string inputStr )
            {
                var dataArray
                    = inputStr.Split (new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                return new LayoutSetting
                {
                    Attrb = dataArray[0],
                    Pos = int.Parse (dataArray[1]),
                    Size = double.Parse (dataArray[2])
                };
            }

            public bool IsRow ( )
            {
                return Attrb == "R";
            }

            public bool IsCol ( )
            {
                return Attrb == "C";
            }
        }
    }

    public enum FlexThumbDir
    {
        HORIZONTAL, VERTICAL, DIAGONAL
    }

    public class FlexThumb : Thumb
    {
        public FlexThumbDir ResizeDirection { get; set; }

        public FlexThumb ( )
        {
            Loaded += ( src, e ) =>
            {
                DragDelta
                    += OnDragDelta;
            };

            Unloaded += ( src, e ) =>
            {
                DragDelta
                    -= OnDragDelta;
            };
        }

        private void OnDragDelta ( object sender, DragDeltaEventArgs e )
        {
            var item = GetParentItem<ListBoxItem> ();
            var panel = GetParentItem<FlexPanel> ();

            if (ResizeDirection == FlexThumbDir.HORIZONTAL)
            {
                panel.OnHorizontalDragDelta (item, e);
            }
            else if (ResizeDirection == FlexThumbDir.VERTICAL)
            {
                panel.OnVerticalDragDelta (item, e);
            }
            else if (ResizeDirection == FlexThumbDir.DIAGONAL)
            {
                panel.OnCornerDragDelta (item, e);
            }
        }

        private T GetParentItem<T> ( )
            where T : DependencyObject
        {
            var dObj = this as DependencyObject;
            while ((dObj = VisualTreeHelper.GetParent (dObj)) != null)
            {
                if (dObj is T)
                {
                    return dObj as T;
                }
            }
            return null;
        }
    }

    public class FlexGrid : Grid
    {
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
                    ( src, e ) =>
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
                    })
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
                    ( src, e ) =>
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
                    })
                );

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
    }
}
