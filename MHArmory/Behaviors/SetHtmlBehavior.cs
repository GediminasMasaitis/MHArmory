using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MHArmory.Behaviors
{
    public static class SetHtmlBehavior
    {
        public static readonly DependencyProperty HtmlProperty = DependencyProperty.RegisterAttached(
            "Html",
            typeof(string),
            typeof(SetHtmlBehavior),
            new FrameworkPropertyMetadata(OnHtmlChanged)
        );

        [AttachedPropertyBrowsableForType(typeof(WebBrowser))]
        public static string GetHtml(WebBrowser target)
        {
            return (string)target.GetValue(HtmlProperty);
        }

        public static void SetHtml(WebBrowser target, string value)
        {
            target.SetValue(HtmlProperty, value);
        }

        static void OnHtmlChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((WebBrowser)sender).NavigateToString(e.NewValue as string);
        }
    }
}
