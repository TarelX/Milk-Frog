using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace MilkFrog.App;

internal static class Theme
{
    internal static Brush Brush(string hex) { var brush = (SolidColorBrush)new BrushConverter().ConvertFrom(hex)!; brush.Freeze(); return brush; }
    internal static readonly Brush Ink = Brush("#35463A"), Muted = Brush("#788175"), Green = Brush("#567B52"), Mint = Brush("#E9EFDF"), Paper = Brush("#FAF9F4"), Line = Brush("#E3E8D9");
    internal static void Install(ResourceDictionary resources)
    {
        var template = (ControlTemplate)XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Button'><Border x:Name='Surface' CornerRadius='8' Background='{TemplateBinding Background}' Padding='{TemplateBinding Padding}' BorderThickness='{TemplateBinding BorderThickness}' BorderBrush='{TemplateBinding BorderBrush}'><ContentPresenter HorizontalAlignment='{TemplateBinding HorizontalContentAlignment}' VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Surface' Property='Opacity' Value='.82'/></Trigger><Trigger Property='IsPressed' Value='True'><Setter TargetName='Surface' Property='Opacity' Value='.65'/></Trigger><Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='Surface' Property='BorderBrush' Value='#86A76D'/><Setter TargetName='Surface' Property='BorderThickness' Value='2'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");
        var style = new Style(typeof(Button));
        style.Setters.Add(new Setter(Control.TemplateProperty, template)); style.Setters.Add(new Setter(Control.BackgroundProperty, Mint));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Ink)); style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 8, 12, 8)));
        style.Setters.Add(new Setter(Control.FontSizeProperty, 12.0)); style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
        style.Setters.Add(new Setter(Control.FocusVisualStyleProperty, resources[SystemParameters.FocusVisualStyleKey]));
        var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false }; disabled.Setters.Add(new Setter(UIElement.OpacityProperty, .45)); style.Triggers.Add(disabled);
        resources[typeof(Button)] = style;
        var textTemplate = (ControlTemplate)XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='TextBox'><Border x:Name='Surface' CornerRadius='9' Background='#FFFEFA' BorderBrush='#D9E1D1' BorderThickness='1' Padding='{TemplateBinding Padding}'><ScrollViewer x:Name='PART_ContentHost'/></Border><ControlTemplate.Triggers><Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='Surface' Property='BorderBrush' Value='#75985D'/><Setter TargetName='Surface' Property='BorderThickness' Value='2'/></Trigger><Trigger Property='IsEnabled' Value='False'><Setter TargetName='Surface' Property='Opacity' Value='.5'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");
        var textStyle = new Style(typeof(TextBox));
        textStyle.Setters.Add(new Setter(Control.TemplateProperty, textTemplate)); textStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.White));
        textStyle.Setters.Add(new Setter(Control.ForegroundProperty, Ink)); textStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 8, 10, 8)));
        textStyle.Setters.Add(new Setter(Control.FontSizeProperty, 13.0)); textStyle.Setters.Add(new Setter(TextBox.CaretBrushProperty, Green));
        resources[typeof(TextBox)] = textStyle;
    }
    internal static TextBlock Text(string value, double size = 12, Brush? color = null, bool bold = false) => new()
    { Text = value, FontSize = size, Foreground = color ?? Ink, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 3) };
    internal static Button Button(string text, Action action, bool primary = false)
    {
        var button = new Button { Content = text, Margin = new Thickness(0, 5, 7, 5), Background = primary ? Green : Mint, Foreground = primary ? Brushes.White : Ink };
        button.Click += (_, _) => action(); return button;
    }
    internal static Border Card(UIElement content, double padding = 15) => new() { Child = content, Background = Brushes.White, BorderBrush = Line, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(padding), Margin = new Thickness(0, 0, 0, 12) };
}
