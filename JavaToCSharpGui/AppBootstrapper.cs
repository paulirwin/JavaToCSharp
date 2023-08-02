using System.Linq;
using JavaToCSharpGui.ViewModels;

namespace JavaToCSharpGui
{
    using System;
    using System.Collections.Generic;
    using Caliburn.Micro;

    public class AppBootstrapper : BootstrapperBase
    {
        SimpleContainer container;

        public AppBootstrapper()
        {
            StartRuntime();
        }

        protected override void PrepareApplication()
        {
            var baseLocate = ViewLocator.LocateTypeForModelType;

            ViewLocator.LocateTypeForModelType = (modelType, displayLocation, context) =>
            {
                var attribute = modelType.GetCustomAttributes(typeof(ViewAttribute), false).OfType<ViewAttribute>().FirstOrDefault(x => x.Context == context);
                return attribute != null ? attribute.ViewType : baseLocate(modelType, displayLocation, context);
            };

            base.PrepareApplication();
        }

        protected override void Configure()
        {
            container = new SimpleContainer();

            container.Singleton<IWindowManager, WindowManager>();
            container.Singleton<IEventAggregator, EventAggregator>();
            container.PerRequest<IShell, ShellViewModel>();
        }

        protected override object GetInstance(Type service, string key)
        {
            var instance = container.GetInstance(service, key);
            if (instance != null)
                return instance;

            throw new InvalidOperationException("Could not locate any instances.");
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance)
        {
            container.BuildUp(instance);
        }

        protected override async void OnStartup(object sender, System.Windows.StartupEventArgs e)
        {
            await DisplayRootViewForAsync<IShell>();
        }
    }
}