﻿using System.Diagnostics;
using System.Windows.Documents;
using System.Windows.Interactivity;
using System.Windows.Navigation;

namespace AGENTIK.Resources
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


        void AssociatedObject_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
