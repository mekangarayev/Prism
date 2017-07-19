﻿using System.Linq;
using Prism.Navigation;
using Prism.Mvvm;
using Prism.Common;
using Xamarin.Forms;
using Prism.Logging;
using Prism.Events;
using Prism.Services;
using DependencyService = Prism.Services.DependencyService;
using Prism.Modularity;
using Autofac;
using Autofac.Features.ResolveAnything;
using Prism.Autofac.Forms.Modularity;
using Prism.Autofac.Navigation;
using Prism.Autofac.Forms;
using Prism.AppModel;

namespace Prism.Autofac
{
    /// <summary>
    /// Application base class using Autofac
    /// </summary>
    public abstract class PrismApplication : PrismApplicationBase<IContainer>
    {
        /// <summary>
        /// Service key used when registering the <see cref="AutofacPageNavigationService"/> with the container
        /// </summary>
        const string _navigationServiceName = "AutofacPageNavigationService";

        /// <summary>
        /// Create a new instance of <see cref="PrismApplication"/>
        /// </summary>
        /// <param name="platformInitializer">Class to initialize platform instances</param>
        /// <remarks>
        /// The method <see cref="IPlatformInitializer.RegisterTypes(IContainer)"/> will be called after <see cref="PrismApplication.RegisterTypes()"/> 
        /// to allow for registering platform specific instances.
        /// </remarks>
        protected PrismApplication(IPlatformInitializer initializer = null)
            : base(initializer)
        {
        }

        protected ContainerBuilder Builder { get; private set; }

        protected virtual ContainerBuilder CreateBuilder() => 
            new ContainerBuilder();

        /// <summary>
        /// Run the bootstrapper process.
        /// </summary>
        public override void Initialize()
        {
            Logger = CreateLogger();

            ModuleCatalog = CreateModuleCatalog();
            ConfigureModuleCatalog();

            Builder = CreateBuilder();

            ConfigureContainer();

            RegisterTypes();

            PlatformInitializer?.RegisterTypes(Builder);
            
            FinishContainerConfiguration();
            Container = CreateContainer();
            
            NavigationService = CreateNavigationService();

            InitializeModules();
        }

        protected override void ConfigureViewModelLocator()
        {
            ViewModelLocationProvider.SetDefaultViewModelFactory((view, type) =>
            {
                NamedParameter parameter = null;
                var page = view as Page;
                if (page != null)
                {
                    parameter = new NamedParameter("navigationService", CreateNavigationService(page));
                }

                return Container.Resolve(type, parameter);
            });
        }

        /// <summary>
        /// Create a default instance of <see cref="IContainer" />
        /// </summary>
        /// <returns>An instance of <see cref="IContainer" /></returns>
        protected override IContainer CreateContainer() => 
            Builder.Build();

        protected override IModuleManager CreateModuleManager()
        {
            return Container.Resolve<IModuleManager>();
        }

        /// <summary>
        /// Create instance of <see cref="INavigationService"/>
        /// </summary>
        /// <remarks>
        /// The <see cref="_navigationServiceKey"/> is used as service key when resolving
        /// </remarks>
        /// <returns>Instance of <see cref="INavigationService"/></returns>
        protected override INavigationService CreateNavigationService()
        {
            return Container.ResolveNamed<INavigationService>(_navigationServiceName);
        }

        protected override void InitializeModules()
        {
            if (ModuleCatalog.Modules.Any())
            {
                var manager = Container.Resolve<IModuleManager>();
                manager.Run();
            }
        }

        protected override void ConfigureContainer()
        {
            Builder.RegisterInstance(Logger).As<ILoggerFacade>().SingleInstance();
            Builder.RegisterInstance(ModuleCatalog).As<IModuleCatalog>().SingleInstance();

            Builder.RegisterType<ApplicationProvider>().As<IApplicationProvider>().SingleInstance();
            Builder.RegisterType<ApplicationStore>().As<IApplicationStore>().SingleInstance();
            Builder.RegisterType<AutofacPageNavigationService>().Named<INavigationService>(_navigationServiceName);
            Builder.RegisterType<ModuleManager>().As<IModuleManager>().SingleInstance();
            Builder.RegisterType<AutofacModuleInitializer>().As<IModuleInitializer>().SingleInstance();
            Builder.RegisterType<EventAggregator>().As<IEventAggregator>().SingleInstance();
            Builder.RegisterType<DependencyService>().As<IDependencyService>().SingleInstance();
            Builder.RegisterType<PageDialogService>().As<IPageDialogService>().SingleInstance();
            Builder.RegisterType<DeviceService>().As<IDeviceService>().SingleInstance();
        }

        /// <summary>
        /// Finish the container's configuration after all other types are registered.
        /// </summary>
        private void FinishContainerConfiguration()
        {
            // Make sure any not specifically registered concrete type can resolve.
            Builder.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
        }
    }
}
