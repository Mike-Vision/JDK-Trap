using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Threading;

namespace JDKTrap.UI.Elements.Controls
{
    [ContentProperty(nameof(InnerContent))]
    public partial class OptionControl : UserControl
    {
        public OptionControl()
        {
            InitializeComponent();
            SetupBindings();
        }

        private void SetupBindings()
        {
            // Binding Header
            Binding headerBinding = new Binding(nameof(Header))
            {
                Source = this,
                TargetNullValue = ""
            };
            HeaderTextBlock.SetBinding(TextBlock.TextProperty, headerBinding);

            // Binding HelpLink visibility container
            Binding helpLinkVisibilityBinding = new Binding(nameof(HelpLink))
            {
                Source = this,
                Converter = (IValueConverter)Resources["NullOrEmptyToVisibilityConverter"]
            };
            HelpLinkContainer.SetBinding(UIElement.VisibilityProperty, helpLinkVisibilityBinding);

            // Binding HelpLink hyperlink parameter
            Binding helpLinkParamBinding = new Binding(nameof(HelpLink))
            {
                Source = this
            };
            HelpHyperlink.SetBinding(Hyperlink.CommandParameterProperty, helpLinkParamBinding);

            // Binding Description text
            Binding descBinding = new Binding(nameof(Description))
            {
                Source = this,
                TargetNullValue = ""
            };
            DescriptionTextBlock.SetBinding(MarkdownTextBlock.MarkdownTextProperty, descBinding);

            // Binding Description visibility
            Binding descVisibilityBinding = new Binding(nameof(Description))
            {
                Source = this,
                Converter = (IValueConverter)Resources["NullOrEmptyToVisibilityConverter"]
            };
            DescriptionTextBlock.SetBinding(UIElement.VisibilityProperty, descVisibilityBinding);

            // Binding InnerContent
            Binding contentBinding = new Binding(nameof(InnerContent))
            {
                Source = this,
                TargetNullValue = ""
            };
            InnerContentPresenter.SetBinding(ContentPresenter.ContentProperty, contentBinding);
        }

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(string), typeof(OptionControl));

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(OptionControl));

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public static readonly DependencyProperty HelpLinkProperty =
            DependencyProperty.Register(nameof(HelpLink), typeof(string), typeof(OptionControl));

        public string HelpLink
        {
            get => (string)GetValue(HelpLinkProperty);
            set => SetValue(HelpLinkProperty, value);
        }

        public static readonly DependencyProperty InnerContentProperty =
            DependencyProperty.Register(nameof(InnerContent), typeof(object), typeof(OptionControl));

        public object InnerContent
        {
            get => GetValue(InnerContentProperty);
            set
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                {
                    SetValue(InnerContentProperty, value);
                }));
            }
        }
    }
}