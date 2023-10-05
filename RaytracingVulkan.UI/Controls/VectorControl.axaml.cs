using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;
using Projektanker.Icons.Avalonia;

namespace RaytracingVulkan.UI.Controls;

[TemplatePart("PART_X", typeof(NumericUpDown))]
[TemplatePart("PART_Y", typeof(NumericUpDown))]
[TemplatePart("PART_Z", typeof(NumericUpDown))]
public class VectorControl : TemplatedControl
{
    public static readonly StyledProperty<decimal?> XProperty = AvaloniaProperty.Register<VectorControl, decimal?>(nameof(X), defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);
    public static readonly StyledProperty<decimal?> YProperty = AvaloniaProperty.Register<VectorControl, decimal?>(nameof(Y), defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);
    public static readonly StyledProperty<decimal?> ZProperty = AvaloniaProperty.Register<VectorControl, decimal?>(nameof(Z), defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);
    
    public decimal? X
    {
        get => GetValue(XProperty);
        set => SetValue(XProperty, value);
    }
    
    public decimal? Y
    {
        get => GetValue(YProperty);
        set => SetValue(YProperty, value);
    }
    
    public decimal? Z
    {
        get => GetValue(ZProperty);
        set => SetValue(ZProperty, value);
    }

    private NumericUpDown? XPart { get; set; }
    private NumericUpDown? YPart { get; set; }
    private NumericUpDown? ZPart { get; set; }

    private IDisposable? _xSubscription, _ySubscription, _zSubscription;
    
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        XPart = e.NameScope.Find<NumericUpDown>("PART_X");
        YPart = e.NameScope.Find<NumericUpDown>("PART_Y");
        ZPart = e.NameScope.Find<NumericUpDown>("PART_Z");
        _xSubscription = XPart?.GetObservable<string>(NumericUpDown.TextProperty!).Subscribe(_ => OnTextChanged());
        _ySubscription = YPart?.GetObservable<string>(NumericUpDown.TextProperty!).Subscribe(_ => OnTextChanged());
        _zSubscription = ZPart?.GetObservable<string>(NumericUpDown.TextProperty!).Subscribe(_ => OnTextChanged());
        base.OnApplyTemplate(e);
    }

    private void OnTextChanged()
    {
        if(XPart is not null) X = Text2Value(XPart.Text);
        if(YPart is not null) Y = Text2Value(YPart.Text);
        if(ZPart is not null) Z = Text2Value(ZPart.Text);
    }

    //could be more friendly to user input
    private decimal? Text2Value(string? text) => decimal.TryParse(text, out var value) ? value : 0;
}