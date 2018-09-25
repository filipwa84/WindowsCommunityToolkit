﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.Toolkit.Uwp.UI.Controls.DataGridInternals;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Microsoft.Toolkit.Uwp.UI.Controls
{
    /// <summary>
    /// Represents a <see cref="DataGrid"/> column that hosts textual content in its cells. In edit mode data can be changed to a value from a collection hosted in a ComboBox.
    /// </summary>
    [StyleTypedProperty(Property = "ElementStyle", StyleTargetType = typeof(TextBlock))]
    [StyleTypedProperty(Property = "EditingElementStyle", StyleTargetType = typeof(ComboBox))]
    public class DataGridComboBoxColumn : DataGridBoundColumn
    {
        private const string DATAGRIDCOMBOBOXCOLUMN_fontFamilyName = "FontFamily";
        private const string DATAGRIDCOMBOBOXCOLUMN_fontSizeName = "FontSize";
        private const string DATAGRIDCOMBOBOXCOLUMN_fontStyleName = "FontStyle";
        private const string DATAGRIDCOMBOBOXCOLUMN_fontWeightName = "FontWeight";
        private const string DATAGRIDCOMBOBOXCOLUMN_foregroundName = "Foreground";
        private const string DATAGRIDCOMBOBOXCOLUMN_itemsSourceName = "ItemsSource";
        private const string DATAGRIDCOMBOBOXCOLUMN_displayMemberPathName = "DisplayMemberPath";
        private const double DATAGRIDCOMBOBOXCOLUMN_leftMargin = 12.0;
        private const double DATAGRIDCOMBOBOXCOLUMN_rightMargin = 12.0;

        private double? _fontSize;
        private DataGrid _owningGrid;
        private FontStyle? _fontStyle;
        private FontWeight? _fontWeight;
        private Brush _foreground;

        /// <summary>
        /// Identifies the ItemsSource dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource", typeof(object), typeof(DataGridComboBoxColumn), new PropertyMetadata(default(object), OnItemSourcePropertyChanged));

        /// <summary>
        /// Gets or sets a collection that is used to generate the content of the ComboBox while in editing mode.
        /// </summary>
        public object ItemsSource
        {
            get { return (object)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        private static void OnItemSourcePropertyChanged(DependencyObject comboBoxDependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var comboColumn = comboBoxDependencyObject as DataGridComboBoxColumn;
            comboColumn.NotifyPropertyChanged(DATAGRIDCOMBOBOXCOLUMN_itemsSourceName);
        }

        /// <summary>
        /// Identifies the DisplayMemberPath dependency property.
        /// </summary>
        public static readonly DependencyProperty DisplayMemberPathProperty = DependencyProperty.Register(
            "DisplayMemberPath", typeof(string), typeof(DataGridComboBoxColumn), new PropertyMetadata(default(string)));

        /// <summary>
        /// Gets or sets the name or path of the property that is displayed in the ComboBox.
        /// </summary>
        public string DisplayMemberPath
        {
            get { return (string)GetValue(DisplayMemberPathProperty); }
            set { SetValue(DisplayMemberPathProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font name.
        /// </summary>
        public FontFamily FontFamily
        {
            get { return (FontFamily)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        /// <summary>
        /// Identifies the FontFamily dependency property.
        /// </summary>
        public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register(
                "FontFamily", typeof(FontFamily), typeof(DataGridComboBoxColumn), new PropertyMetadata(null, OnFontFamilyPropertyChanged));

        private static void OnFontFamilyPropertyChanged(DependencyObject comboBoxColumnDependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var comboColumn = comboBoxColumnDependencyObject as DataGridComboBoxColumn;
            comboColumn.NotifyPropertyChanged(DATAGRIDCOMBOBOXCOLUMN_fontFamilyName);
        }

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        // Use DefaultValue here so undo in the Designer will set this to NaN
        [DefaultValue(double.NaN)]
        public double FontSize
        {
            get
            {
                return _fontSize ?? double.NaN;
            }

            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    NotifyPropertyChanged(DATAGRIDCOMBOBOXCOLUMN_fontSizeName);
                }
            }
        }

        /// <summary>
        /// Gets or sets the font style.
        /// </summary>
        public FontStyle FontStyle
        {
            get
            {
                return _fontStyle ?? FontStyle.Normal;
            }

            set
            {
                if (_fontStyle != value)
                {
                    _fontStyle = value;
                    NotifyPropertyChanged(DATAGRIDCOMBOBOXCOLUMN_fontStyleName);
                }
            }
        }

        /// <summary>
        /// Gets or sets the font weight or thickness.
        /// </summary>
        public FontWeight FontWeight
        {
            get
            {
                return _fontWeight ?? FontWeights.Normal;
            }

            set
            {
                if (!_fontWeight.HasValue || _fontWeight.Value.Weight != value.Weight)
                {
                    _fontWeight = value;
                    NotifyPropertyChanged(DATAGRIDCOMBOBOXCOLUMN_fontWeightName);
                }
            }
        }

        /// <summary>
        /// Gets or sets a brush that describes the foreground of the column cells.
        /// </summary>
        public Brush Foreground
        {
            get
            {
                return _foreground;
            }

            set
            {
                if (_foreground != value)
                {
                    _foreground = value;
                    NotifyPropertyChanged(DATAGRIDCOMBOBOXCOLUMN_foregroundName);
                }
            }
        }

        /// <summary>
        /// Gets a <see cref="T:Windows.UI.Xaml.Controls.ComboBox"/> control that is bound to the column's ItemsSource collection.
        /// </summary>
        /// <param name="cell">The cell that will contain the generated element.</param>
        /// <param name="dataItem">The data item represented by the row that contains the intended cell.</param>
        /// <returns>A new <see cref="T:Windows.UI.Xaml.Controls.ComboBox"/> control that is bound to the column's ItemsSource collection.</returns>
        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            var value = dataItem.GetType().GetProperty(Binding.Path.Path).GetValue(dataItem);

            var items = ItemsSource as IEnumerable<object>;

            var selection = DisplayMemberPath != null
                ? items?.FirstOrDefault(x => x.GetType().GetProperty(Binding.Path.Path).GetValue(x).Equals(value))
                : items?.FirstOrDefault(x => x.Equals(value));

            var comboBox = new ComboBox
            {
                SelectedItem = selection,
                Margin = default(Thickness),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };

            var itemsSourceBinding = new Binding
            {
                Source = this,
                Path = new PropertyPath(DATAGRIDCOMBOBOXCOLUMN_itemsSourceName)
            };

            var displayMemberPathBinding = new Binding
            {
                Source = this,
                Path = new PropertyPath(DATAGRIDCOMBOBOXCOLUMN_displayMemberPathName)
            };

            comboBox.SetBinding(ComboBox.ItemsSourceProperty, itemsSourceBinding);
            comboBox.SetBinding(ComboBox.DisplayMemberPathProperty, displayMemberPathBinding);

            if (DependencyProperty.UnsetValue != ReadLocalValue(DataGridComboBoxColumn.FontFamilyProperty))
            {
                comboBox.FontFamily = this.FontFamily;
            }

            if (_fontSize.HasValue)
            {
                comboBox.FontSize = _fontSize.Value;
            }

            if (_fontStyle.HasValue)
            {
                comboBox.FontStyle = _fontStyle.Value;
            }

            if (_fontWeight.HasValue)
            {
                comboBox.FontWeight = _fontWeight.Value;
            }

            RefreshForeground(comboBox, (cell != null & cell.OwningRow != null) ? cell.OwningRow.ComputedForeground : null);

            comboBox.SelectionChanged += (sender, args) =>
            {
                var item = args.AddedItems.FirstOrDefault();
                if (item != null)
                {
                    var newValue = DisplayMemberPath != null
                        ? item.GetType().GetProperty(Binding.Path.Path).GetValue(item)
                        : item;

                    dataItem.GetType().GetProperty(Binding.Path.Path).SetValue(dataItem, newValue);
                }
            };

            return comboBox;
        }

        /// <summary>
        /// Gets a read-only <see cref="T:Windows.UI.Xaml.Controls.TextBlock"/> element that is bound to the column's <see cref="P:Microsoft.Toolkit.Uwp.UI.Controls.DataGridBoundColumn.Binding"/> property value.
        /// </summary>
        /// <param name="cell">The cell that will contain the generated element.</param>
        /// <param name="dataItem">The data item represented by the row that contains the intended cell.</param>
        /// <returns>A new, read-only <see cref="T:Windows.UI.Xaml.Controls.TextBlock"/> element that is bound to the column's <see cref="P:Microsoft.Toolkit.Uwp.UI.Controls.DataGridBoundColumn.Binding"/> property value.</returns>
        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            var textBlockElement = new TextBlock
            {
                Margin = new Thickness(DATAGRIDCOMBOBOXCOLUMN_leftMargin, 0.0, DATAGRIDCOMBOBOXCOLUMN_rightMargin, 0.0),
                VerticalAlignment = VerticalAlignment.Center
            };

            if (DependencyProperty.UnsetValue != ReadLocalValue(DataGridComboBoxColumn.FontFamilyProperty))
            {
                textBlockElement.FontFamily = this.FontFamily;
            }

            if (_fontSize.HasValue)
            {
                textBlockElement.FontSize = _fontSize.Value;
            }

            if (_fontStyle.HasValue)
            {
                textBlockElement.FontStyle = _fontStyle.Value;
            }

            if (_fontWeight.HasValue)
            {
                textBlockElement.FontWeight = _fontWeight.Value;
            }

            RefreshForeground(textBlockElement, (cell != null & cell.OwningRow != null) ? cell.OwningRow.ComputedForeground : null);

            bool isDesignMode = Windows.ApplicationModel.DesignMode.DesignModeEnabled;

            if ((this.Binding != null || !isDesignMode) && EnsureOwningGrid())
            {
                if (DisplayMemberPath == null)
                {
                    textBlockElement.SetBinding(TextBlock.TextProperty, this.Binding);
                }
            }

            return textBlockElement;
        }

        /// <summary>
        /// Causes the column cell being edited to revert to the specified value.
        /// </summary>
        /// <param name="editingElement">The element that the column displays for a cell in editing mode.</param>
        /// <param name="uneditedValue">The previous, unedited value in the cell being edited.</param>
        protected override void CancelCellEdit(FrameworkElement editingElement, object uneditedValue)
        {
            var comboBox = editingElement as ComboBox;
            if (comboBox != null && uneditedValue != null)
            {
                var value = uneditedValue.GetType().GetProperty(Binding.Path.Path).GetValue(uneditedValue);
                var items = ItemsSource as IEnumerable<object>;
                var selection = items?.FirstOrDefault(x => x.GetType().GetProperty(Binding.Path.Path).GetValue(x).Equals(value));

                comboBox.SelectedItem = selection;
            }
            else if (comboBox != null)
            {
                comboBox.SelectedItem = null;
            }
        }

        /// <summary>
        /// Called when the cell in the column enters editing mode.
        /// </summary>
        /// <param name="editingElement">The element that the column displays for a cell in editing mode.</param>
        /// <param name="editingEventArgs">Information about the user gesture that is causing a cell to enter editing mode.</param>
        /// <returns>The unedited value. </returns>
        protected override object PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
        {
            return (editingElement as ComboBox)?.SelectedItem;
        }

        /// <summary>
        /// Called by the DataGrid control when this column asks for its elements to be updated, because a property changed.
        /// </summary>
        protected internal override void RefreshCellContent(FrameworkElement element, Brush computedRowForeground, string propertyName)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            var comboBox = element as ComboBox;
            if (comboBox == null)
            {
                var textBlock = element as TextBlock;
                if (textBlock == null)
                {
                    throw DataGridError.DataGrid.ValueIsNotAnInstanceOfEitherOr(nameof(element), typeof(ComboBox), typeof(TextBlock));
                }

                if (propertyName == DATAGRIDCOMBOBOXCOLUMN_fontFamilyName)
                {
                    textBlock.FontFamily = this.FontFamily;
                }
                else if (propertyName == DATAGRIDCOMBOBOXCOLUMN_fontSizeName)
                {
                    SetTextFontSize(textBlock, TextBlock.FontSizeProperty);
                }
                else if (propertyName == DATAGRIDCOMBOBOXCOLUMN_fontStyleName)
                {
                    textBlock.FontStyle = this.FontStyle;
                }
                else if (propertyName == DATAGRIDCOMBOBOXCOLUMN_fontWeightName)
                {
                    textBlock.FontWeight = this.FontWeight;
                }
                else if (propertyName == DATAGRIDCOMBOBOXCOLUMN_foregroundName)
                {
                    RefreshForeground(textBlock, computedRowForeground);
                }
                else if (propertyName == DATAGRIDCOMBOBOXCOLUMN_itemsSourceName)
                {
                    OwningGrid.OnColumnBindingChanged(this);
                }
                else
                {
                    if (this.FontFamily != null)
                    {
                        textBlock.FontFamily = this.FontFamily;
                    }

                    SetTextFontSize(textBlock, TextBlock.FontSizeProperty);
                    textBlock.FontStyle = this.FontStyle;
                    textBlock.FontWeight = this.FontWeight;
                    RefreshForeground(textBlock, computedRowForeground);
                }

                return;
            }

            if (propertyName == DATAGRIDCOMBOBOXCOLUMN_fontFamilyName)
            {
                comboBox.FontFamily = this.FontFamily;
            }
            else if (propertyName == DATAGRIDCOMBOBOXCOLUMN_fontSizeName)
            {
                SetTextFontSize(comboBox, ComboBox.FontSizeProperty);
            }
            else if (propertyName == DATAGRIDCOMBOBOXCOLUMN_fontStyleName)
            {
                comboBox.FontStyle = this.FontStyle;
            }
            else if (propertyName == DATAGRIDCOMBOBOXCOLUMN_fontWeightName)
            {
                comboBox.FontWeight = this.FontWeight;
            }
            else if (propertyName == DATAGRIDCOMBOBOXCOLUMN_foregroundName)
            {
                RefreshForeground(comboBox, computedRowForeground);
            }
            else
            {
                if (this.FontFamily != null)
                {
                    comboBox.FontFamily = this.FontFamily;
                }

                SetTextFontSize(comboBox, ComboBox.FontSizeProperty);
                comboBox.FontStyle = this.FontStyle;
                comboBox.FontWeight = this.FontWeight;
                RefreshForeground(comboBox, computedRowForeground);
            }
        }

        /// <summary>
        /// Called when the computed foreground of a row changed.
        /// </summary>
        protected internal override void RefreshForeground(FrameworkElement element, Brush computedRowForeground)
        {
            if (element is ComboBox comboBox)
            {
                RefreshForeground(comboBox, computedRowForeground);
            }
            else if (element is TextBlock textBlock)
            {
                RefreshForeground(textBlock, computedRowForeground);
            }
        }

        private void RefreshForeground(ComboBox comboBox, Brush computedRowForeground)
        {
            if (this.Foreground == null)
            {
                if (computedRowForeground != null)
                {
                    comboBox.Foreground = computedRowForeground;
                }
            }
            else
            {
                comboBox.Foreground = this.Foreground;
            }
        }

        private void RefreshForeground(TextBlock textBlock, Brush computedRowForeground)
        {
            if (this.Foreground == null)
            {
                if (computedRowForeground != null)
                {
                    textBlock.Foreground = computedRowForeground;
                }
            }
            else
            {
                textBlock.Foreground = this.Foreground;
            }
        }

        private void SetTextFontSize(DependencyObject textElement, DependencyProperty fontSizeProperty)
        {
            double newFontSize = this.FontSize;
            if (double.IsNaN(newFontSize))
            {
                textElement.ClearValue(fontSizeProperty);
            }
            else
            {
                textElement.SetValue(fontSizeProperty, newFontSize);
            }
        }

        private bool EnsureOwningGrid()
        {
            if (this.OwningGrid != null)
            {
                if (this.OwningGrid != _owningGrid)
                {
                    _owningGrid = this.OwningGrid;
                    _owningGrid.LoadingRow += OwningGrid_LoadingRow;
                    _owningGrid.CellEditEnded += OwningGrid_CellEditEnded;
                }

                return true;
            }

            return false;
        }

        private void OwningGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            SetDisplayMemberPathValue(e.Row);
        }

        private void OwningGrid_CellEditEnded(object sender, DataGridCellEditEndedEventArgs e)
        {
            SetDisplayMemberPathValue(e.Row);
        }

        private void SetDisplayMemberPathValue(DataGridRow row)
        {
            if (this.OwningGrid != null)
            {
                var textBlock = this.GetCellContent(row) as TextBlock;
                if (textBlock != null && DisplayMemberPath != null)
                {
                    var displayValue = GetDisplayValue(row.DataContext);

                    textBlock.Text = displayValue as string ?? displayValue.ToString();
                }
            }
        }

        private object GetDisplayValue(object dataItem)
        {
            var value = dataItem.GetType().GetProperty(Binding?.Path.Path).GetValue(dataItem);

            var items = ItemsSource as IEnumerable<object>;

            var item = items?.FirstOrDefault(x => x.GetType().GetProperty(Binding.Path.Path).GetValue(x).Equals(value));

            var displayValue = item?.GetType().GetProperty(DisplayMemberPath).GetValue(item);

            return displayValue;
        }
    }
}