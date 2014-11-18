using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using BRS.CRE.SPEAR.DAL.Tools.LINQ;
using BRS.CRE.SPEAR.DAL.Tools.UDF;
using BRS.CRE.SPEAR.PL.Core.UI.Util;

namespace Vbongithub
{
    public class FlexPanel : ListBox
    {
        #region Properties

        public ObservableCollection<RowDefinition> RowDefinitions { get; set; }

        public ObservableCollection<ColumnDefinition> ColumnDefinitions { get; set; }

        public string ViewPersistKey
        {
            get { return GetValue (ViewPersistKeyProperty) as string; }
            set { SetValue (ViewPersistKeyProperty, value); }
        }

        public static readonly DependencyProperty ViewPersistKeyProperty
            = DependencyProperty.Register (
                "ViewPersistKey",
                typeof (string),
                typeof (FlexPanel),
                new FrameworkPropertyMetadata (null)
                );

        #endregion Properties

        #region Constructor

        static FlexPanel ( )
        {
            DefaultStyleKeyProperty.OverrideMetadata (
                typeof (FlexPanel),
                new FrameworkPropertyMetadata (typeof (FlexPanel))
                );
        }

        public FlexPanel ( )
        {
            RowDefinitions
                = new ObservableCollection<RowDefinition> ();
            ColumnDefinitions
                = new ObservableCollection<ColumnDefinition> ();

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

        #endregion Constructor

        #region Util

        private void SaveLayout ( )
        {
            if (string.IsNullOrWhiteSpace (ViewPersistKey)) return;

            if (Properties.Settings.Default.FlexPanelSettings == null)
            {
                Properties.Settings.Default.FlexPanelSettings
                    = new System.Collections.Specialized.StringCollection ();
            }

            Func<string, string> getKey = ( str )
                => str.Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault ();

            var settings
                = Properties.Settings.Default.FlexPanelSettings
                    .Cast<string> ()
                    .Where (func => getKey (func) != ViewPersistKey)
                    .ToList ();

            settings.Add (
                LayoutSetting.Encode (this)
                );

            Properties.Settings.Default.FlexPanelSettings.Clear ();

            Properties.Settings.Default.FlexPanelSettings.AddRange (
                settings.ToArray ()
                );

            Properties.Settings.Default.Save ();
        }

        private void LoadLayout ( )
        {
            Properties.Settings.Default.Reload ();

            if (Properties.Settings.Default.FlexPanelSettings == null
               || string.IsNullOrWhiteSpace (ViewPersistKey)
               ) return;

            var settings
                = Properties.Settings.Default.FlexPanelSettings
                    .Cast<string> ()
                    .Select (LayoutSetting.Decode)
                    .FirstOrDefault (func => func.Key == ViewPersistKey);

            if (settings != null)
            {
                Predicate<double> isValid
                    = ( data ) => !(double.IsNaN (data) || double.IsInfinity (data));

                settings.RowDefs
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

                settings.ColDefs
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
        }

        internal void OnHorizontalDragDelta ( ListBoxItem sender, DragDeltaEventArgs e )
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

            SaveLayout ();
        }

        internal void OnVerticalDragDelta ( ListBoxItem sender, DragDeltaEventArgs e )
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

            SaveLayout ();
        }

        internal void OnCornerDragDelta ( ListBoxItem sender, DragDeltaEventArgs e )
        {
            OnHorizontalDragDelta (sender, e);
            OnVerticalDragDelta (sender, e);
        }

        private void OnSizeChanged ( object sender, SizeChangedEventArgs e )
        {
            if (!RowDefinitions.IsNullOrEmpty ())
            {
                var totalHeight
                    = RowDefinitions
                        .Select (func => func.ActualHeight)
                        .Sum ();

                if (!totalHeight.IsZero ())
                {
                    RowDefinitions
                        .Select (func => new
                        {
                            RowDef = func,
                            NewHeight = new GridLength (func.ActualHeight / totalHeight, GridUnitType.Star)
                        })
                        .ToList ()
                        .ForEach (func => func.RowDef.Height = func.NewHeight);
                }
            }

            if (!ColumnDefinitions.IsNullOrEmpty ())
            {
                var totalWidth
                    = ColumnDefinitions
                        .Select (func => func.ActualWidth)
                        .Sum ();

                if (!totalWidth.IsZero ())
                {
                    ColumnDefinitions
                        .Select (func => new
                        {
                            ColDef = func,
                            NewWidth = new GridLength (func.ActualWidth / totalWidth, GridUnitType.Star)
                        })
                        .ToList ()
                        .ForEach (func => func.ColDef.Width = func.NewWidth);
                }
            }

            SaveLayout ();
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
            public string Key { get; private set; }

            public IList<LayoutDef.RowDef> RowDefs { get; private set; }

            public IList<LayoutDef.ColDef> ColDefs { get; private set; }

            private LayoutSetting ( )
            {
            }

            public static LayoutSetting Decode ( string inputString )
            {
                var data
                    = inputString.Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                var gridDefs
                    = data.Skip (1)
                        .Select (LayoutDef.Parse)
                        .ToList ();

                var layout
                    = new LayoutSetting
                    {
                        Key = data[0],
                        RowDefs = gridDefs.OfType<LayoutDef.RowDef> ().ToList (),
                        ColDefs = gridDefs.OfType<LayoutDef.ColDef> ().ToList (),
                    };

                return layout;
            }

            public static string Encode ( FlexPanel panel )
            {
                var layoutSetting
                    = new StringBuilder (panel.ViewPersistKey)
                        .Append (",");

                if (panel.RowDefinitions != null)
                {
                    var totalHeight
                       = panel.RowDefinitions
                           .Select (func => func.ActualHeight)
                           .Sum ();

                    layoutSetting.Append (
                        string.Join (
                            ",",
                            panel.RowDefinitions.Select (
                                ( func, pos ) =>
                                    string.Format ("R:{0}:{1}", pos, Math.Round (func.ActualHeight / totalHeight, 4))
                                )
                            )
                        );
                }

                layoutSetting.Append (",");

                if (panel.ColumnDefinitions != null)
                {
                    var totalWidth
                        = panel.ColumnDefinitions
                            .Select (func => func.ActualWidth)
                            .Sum ();

                    layoutSetting.Append (
                        string.Join (
                            ",",
                            panel.ColumnDefinitions.Select (
                                ( func, pos ) =>
                                    string.Format ("C:{0}:{1}", pos, Math.Round (func.ActualWidth / totalWidth, 4))
                                )
                            )
                        );
                }

                return layoutSetting.ToString ();
            }

            internal abstract class LayoutDef
            {
                public int Pos { get; private set; }

                public double Size { get; private set; }

                public static LayoutDef Parse ( string inputStr )
                {
                    var dataArray
                        = inputStr
                            .Split (new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries)
                            .ToArray ();

                    if (dataArray.Length == 3)
                    {
                        var pos = int.Parse (dataArray[1]);
                        var size = double.Parse (dataArray[2]);

                        if (dataArray[0] == RowDef.Tag)
                        {
                            return new RowDef
                            {
                                Pos = pos,
                                Size = size
                            };
                        }
                        else if (dataArray[0] == ColDef.Tag)
                        {
                            return new ColDef
                            {
                                Pos = pos,
                                Size = size
                            };
                        }
                    }

                    throw new Exception ("Invalid row/column layout details.");
                }

                internal class RowDef : LayoutDef
                {
                    public const string Tag = "R";
                }

                internal class ColDef : LayoutDef
                {
                    public const string Tag = "C";
                }
            }
        }

        #endregion Util
    }

    public enum FlexPanelThumbDirection
    {
        HORIZONTAL, VERTICAL, DIAGONAL
    }

    public class FlexPanelThumb : Thumb
    {
        #region Properties

        public FlexPanelThumbDirection ResizeDirection
        {
            get { return (FlexPanelThumbDirection)GetValue (ResizeDirectionProperty); }
            set { SetValue (ResizeDirectionProperty, value); }
        }

        public static readonly DependencyProperty ResizeDirectionProperty
            = DependencyProperty.Register (
                "ResizeDirection",
                typeof (FlexPanelThumbDirection),
                typeof (FlexPanelThumb),
                new FrameworkPropertyMetadata (FlexPanelThumbDirection.DIAGONAL, FrameworkPropertyMetadataOptions.AffectsRender)
                );

        #endregion Properties

        #region Constructor

        public FlexPanelThumb ( )
        {
            Loaded += ( src, e ) =>
                {
                    DragDelta
                        += OnDragDelta;

                    OnLoaded ();
                };

            Unloaded += ( src, e ) =>
                {
                    DragDelta
                        -= OnDragDelta;
                };
        }

        #endregion Constructor

        #region Util

        private void OnLoaded ( )
        {
            var item
                = VisualTreeFinder.FindParentControl<ListBoxItem> (this);
            var panel
                = VisualTreeFinder.FindParentControl<FlexPanel> (this);

            if (panel != null
                && item != null
                && panel.ColumnDefinitions != null
                && panel.RowDefinitions != null
                )
            {
                var colPos
                        = NumericUtil.Sum (
                            item.GetValue (Grid.ColumnProperty) as int?,
                            (item.GetValue (Grid.ColumnSpanProperty) as int?)
                            );
                var rowPos
                    = NumericUtil.Sum (
                        item.GetValue (Grid.RowProperty) as int?,
                        (item.GetValue (Grid.RowSpanProperty) as int?)
                        );

                if (ResizeDirection == FlexPanelThumbDirection.HORIZONTAL)
                {
                    Visibility
                        = (colPos >= panel.ColumnDefinitions.Count)
                            ? Visibility.Collapsed
                            : Visibility.Visible;
                }
                else if (ResizeDirection == FlexPanelThumbDirection.VERTICAL)
                {
                    Visibility
                        = (rowPos >= panel.RowDefinitions.Count)
                            ? Visibility.Collapsed
                            : Visibility.Visible;
                }
                else if (ResizeDirection == FlexPanelThumbDirection.DIAGONAL)
                {
                    Visibility
                        = (colPos >= panel.ColumnDefinitions.Count) || (rowPos >= panel.RowDefinitions.Count)
                            ? Visibility.Collapsed
                            : Visibility.Visible;
                }
            }
        }

        private void OnDragDelta ( object sender, DragDeltaEventArgs e )
        {
            var item = VisualTreeFinder.FindParentControl<ListBoxItem> (this);
            var panel = VisualTreeFinder.FindParentControl<FlexPanel> (this);

            if (panel != null)
            {
                if (ResizeDirection == FlexPanelThumbDirection.HORIZONTAL)
                {
                    panel.OnHorizontalDragDelta (item, e);
                }
                else if (ResizeDirection == FlexPanelThumbDirection.VERTICAL)
                {
                    panel.OnVerticalDragDelta (item, e);
                }
                else if (ResizeDirection == FlexPanelThumbDirection.DIAGONAL)
                {
                    panel.OnCornerDragDelta (item, e);
                }
            }
        }

        #endregion Util
    }
}
