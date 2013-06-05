using System.Diagnostics;
using System.Windows.Documents;
using System.Windows.Interactivity;

namespace AGENTIK
{
    public class HyperlinkNavigationBehavior : Behavior<Hyperlink>
    {
        protected override void OnAttached()
        {
            AssociatedObject.RequestNavigate += AssociatedObject_RequestNavigate;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.RequestNavigate -= AssociatedObject_RequestNavigate;
        }


        void AssociatedObject_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
