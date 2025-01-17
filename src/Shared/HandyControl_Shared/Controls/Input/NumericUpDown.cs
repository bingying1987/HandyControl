﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HandyControl.Data;
using HandyControl.Interactivity;
using HandyControl.Tools;

namespace HandyControl.Controls
{
    /// <inheritdoc cref="IDataInput" />
    /// <summary>
    ///     数值选择控件
    /// </summary>
    [TemplatePart(Name = ElementTextBox, Type = typeof(TextBox))]
    public class NumericUpDown : Control, IDataInput
    {
        #region Constants

        private const string ElementTextBox = "PART_TextBox";

        #endregion Constants

        #region Data

        private TextBox _textBox;

        #endregion Data

        public NumericUpDown()
        {
            CommandBindings.Add(new CommandBinding(ControlCommands.Prev, (s, e) =>
            {
                Value += Increment;
                _textBox.Text = CurrentText;
                _textBox.Select(_textBox.Text.Length, 0);
            }));
            CommandBindings.Add(new CommandBinding(ControlCommands.Next, (s, e) =>
            {
                Value -= Increment;
                _textBox.Text = CurrentText;
                _textBox.Select(_textBox.Text.Length, 0);
            }));
            CommandBindings.Add(new CommandBinding(ControlCommands.Clear, (s, e) =>
            {
                ClearValue(ValueProperty);
                _textBox.Text = string.Empty;
            }));

            Loaded += (s, e) => OnApplyTemplate();
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            if (_textBox != null)
            {
                _textBox?.Focus();
                _textBox.Select(_textBox.Text.Length, 0);
            }
        }

        public override void OnApplyTemplate()
        {
            if (_textBox != null)
            {
                _textBox.TextChanged -= TextBox_TextChanged;
                _textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
                _textBox.LostFocus -= TextBox_LostFocus;
            }

            base.OnApplyTemplate();

            _textBox = GetTemplateChild(ElementTextBox) as TextBox;

            if (_textBox != null)
            {
                _textBox.TextChanged += TextBox_TextChanged;
                _textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                _textBox.LostFocus += TextBox_LostFocus;
                _textBox.Text = CurrentText;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (IsError) return;
            _textBox.Text = CurrentText;
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                Value += Increment;
                _textBox.Text = CurrentText;
            }
            else if (e.Key == Key.Down)
            {
                Value -= Increment;
                _textBox.Text = CurrentText;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!VerifyData()) return;
            if (double.TryParse(_textBox.Text, out var value))
            {
                Value = value;
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            if (_textBox.IsFocused)
            {
                Value += e.Delta > 0 ? Increment : -Increment;
                _textBox.Text = CurrentText;
                _textBox.Select(_textBox.Text.Length, 0);
                e.Handled = true;
            }
        }

        private string CurrentText => DecimalPlaces.HasValue
            ? Value.ToString($"#0.{new string('0', DecimalPlaces.Value)}")
            : Value.ToString("#0");

        protected virtual void OnValueChanged(FunctionEventArgs<double> e) => RaiseEvent(e);

        /// <summary>
        ///     值改变事件
        /// </summary>
        public static readonly RoutedEvent ValueChangedEvent =
            EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble,
                typeof(EventHandler<FunctionEventArgs<double>>), typeof(NumericUpDown));

        /// <summary>
        ///     值改变事件
        /// </summary>
        public event EventHandler<FunctionEventArgs<double>> ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }

        /// <summary>
        ///     当前值
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(double), typeof(NumericUpDown),
            new FrameworkPropertyMetadata(ValueBoxes.Double0Box, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnValueChanged, CoerceValue), ValidateHelper.IsInRangeOfDouble);

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctl = (NumericUpDown) d;
            var v = (double) e.NewValue;
            if (ctl._textBox != null)
            {
                ctl._textBox.Text = v.ToString();
            }

            ctl.OnValueChanged(new FunctionEventArgs<double>(ValueChangedEvent, ctl)
            {
                Info = v
            });
        }

        private static object CoerceValue(DependencyObject d, object basevalue)
        {
            var ctl = (NumericUpDown)d;
            var minimum = ctl.Minimum;
            var num = (double)basevalue;
            if (num < minimum)
            {
                ctl.Value = minimum;
                return minimum;
            }
            var maximum = ctl.Maximum;
            if (num > maximum)
            {
                ctl.Value = maximum;
            }

            var result = num > maximum ? maximum : num;
            if (!ctl.DecimalPlaces.HasValue)
            {
                result = Math.Floor(result);
            }

            return result;
        }

        /// <summary>
        ///     当前值
        /// </summary>
        public double Value
        {
            get => (double) GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>
        ///     最大值
        /// </summary>
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            "Maximum", typeof(double), typeof(NumericUpDown), new PropertyMetadata(double.MaxValue, OnMaximumChanged, CoerceMaximum), ValidateHelper.IsInRangeOfDouble);

        private static void OnMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctl = (NumericUpDown)d;
            ctl.CoerceValue(MinimumProperty);
            ctl.CoerceValue(ValueProperty);
        }

        private static object CoerceMaximum(DependencyObject d, object basevalue)
        {
            var minimum = ((NumericUpDown)d).Minimum;
            return (double)basevalue < minimum ? minimum : basevalue;
        }

        /// <summary>
        ///     最大值
        /// </summary>
        public double Maximum
        {
            get => (double) GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        /// <summary>
        ///     最小值
        /// </summary>
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
            "Minimum", typeof(double), typeof(NumericUpDown), new PropertyMetadata(ValueBoxes.Double0Box, OnMinimumChanged, CoerceMinimum), ValidateHelper.IsInRangeOfDouble);

        private static void OnMinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctl = (NumericUpDown)d;
            ctl.CoerceValue(MaximumProperty);
            ctl.CoerceValue(ValueProperty);
        }

        private static object CoerceMinimum(DependencyObject d, object basevalue)
        {
            var maximum = ((NumericUpDown)d).Maximum;
            return (double)basevalue > maximum ? maximum : basevalue;
        }

        /// <summary>
        ///     最小值
        /// </summary>
        public double Minimum
        {
            get => (double) GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        /// <summary>
        ///     指示每单击一下按钮时增加或减少的数量
        /// </summary>
        public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register(
            "Increment", typeof(double), typeof(NumericUpDown), new PropertyMetadata(ValueBoxes.Double1Box));

        /// <summary>
        ///     指示每单击一下按钮时增加或减少的数量
        /// </summary>
        public double Increment
        {
            get => (double) GetValue(IncrementProperty);
            set => SetValue(IncrementProperty, value);
        }

        /// <summary>
        ///     指示要显示的小数位数
        /// </summary>
        public static readonly DependencyProperty DecimalPlacesProperty = DependencyProperty.Register(
            "DecimalPlaces", typeof(int?), typeof(NumericUpDown), new PropertyMetadata(default(int?)));

        /// <summary>
        ///     指示要显示的小数位数
        /// </summary>
        public int? DecimalPlaces
        {
            get => (int?) GetValue(DecimalPlacesProperty);
            set => SetValue(DecimalPlacesProperty, value);
        }

        /// <summary>
        ///     是否显示上下调值按钮
        /// </summary>
        internal static readonly DependencyProperty ShowUpDownButtonProperty = DependencyProperty.Register(
            "ShowUpDownButton", typeof(bool), typeof(NumericUpDown), new PropertyMetadata(ValueBoxes.TrueBox));

        /// <summary>
        ///     是否显示上下调值按钮
        /// </summary>
        internal bool ShowUpDownButton
        {
            get => (bool) GetValue(ShowUpDownButtonProperty);
            set => SetValue(ShowUpDownButtonProperty, value);
        }

        /// <summary>
        ///     数据是否错误
        /// </summary>
        public static readonly DependencyProperty IsErrorProperty = DependencyProperty.Register(
            "IsError", typeof(bool), typeof(NumericUpDown), new PropertyMetadata(ValueBoxes.FalseBox));

        public bool IsError
        {
            get => (bool) GetValue(IsErrorProperty);
            set => SetValue(IsErrorProperty, value);
        }

        /// <summary>
        ///     错误提示
        /// </summary>
        public static readonly DependencyProperty ErrorStrProperty = DependencyProperty.Register(
            "ErrorStr", typeof(string), typeof(NumericUpDown), new PropertyMetadata(default(string)));

        public string ErrorStr
        {
            get => (string) GetValue(ErrorStrProperty);
            set => SetValue(ErrorStrProperty, value);
        }

        public static readonly DependencyPropertyKey TextTypePropertyKey =
            DependencyProperty.RegisterReadOnly("TextType", typeof(TextType), typeof(NumericUpDown),
                new PropertyMetadata(default(TextType)));

        /// <summary>
        ///     文本类型
        /// </summary>
        public static readonly DependencyProperty TextTypeProperty = TextTypePropertyKey.DependencyProperty;

        public TextType TextType
        {
            get => (TextType) GetValue(TextTypeProperty);
            set => SetValue(TextTypeProperty, value);
        }

        /// <summary>
        ///     是否显示清除按钮
        /// </summary>
        public static readonly DependencyProperty ShowClearButtonProperty = DependencyProperty.Register(
            "ShowClearButton", typeof(bool), typeof(NumericUpDown), new PropertyMetadata(ValueBoxes.FalseBox));

        public bool ShowClearButton
        {
            get => (bool) GetValue(ShowClearButtonProperty);
            set => SetValue(ShowClearButtonProperty, value);
        }

        public Func<string, OperationResult<bool>> VerifyFunc { get; set; }

        public virtual bool VerifyData()
        {
            OperationResult<bool> result;

            if (VerifyFunc != null)
            {
                result = VerifyFunc.Invoke(_textBox.Text);
            }
            else
            {
                if (!string.IsNullOrEmpty(_textBox.Text))
                {
                    if (double.TryParse(_textBox.Text, out var value))
                    {
                        if (value < Minimum || value > Maximum)
                        {
                            result = OperationResult.Failed(Properties.Langs.Lang.OutOfRange);
                        }
                        else
                        {
                            result = OperationResult.Success();
                        }
                    }
                    else
                    {
                        result = OperationResult.Failed(Properties.Langs.Lang.FormatError);
                    }
                }
                else if (InfoElement.GetNecessary(this))
                {
                    result = OperationResult.Failed(Properties.Langs.Lang.IsNecessary);
                }
                else
                {
                    result = OperationResult.Success();
                }
            }

            IsError = !result.Data;
            ErrorStr = result.Message;
            return result.Data;
        }
    }
}