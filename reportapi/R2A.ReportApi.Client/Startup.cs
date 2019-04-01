using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using IDD.Infrastructure;
using MediatR;
using MediatR.Pipeline;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Rewrite.Internal.PatternSegments;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using R2A.ReportApi.Client.Authentication.Certificate;
using R2A.ReportApi.Client.Common;
using R2A.ReportApi.Client.Infrastructure;
using R2A.ReportApi.Client.Infrastructure.Errors;
using R2A.ReportApi.Client.Infrastructure.Logging;
using RabbitMQ.Client;
using SimpleInjector;
using SimpleInjector.Integration.AspNetCore.Mvc;
using SimpleInjector.Lifestyles;

namespace R2A.ReportApi.Client
{
    public class Startup
    {
        private readonly Container _container = new Container();
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = Authentication.AuthenticationScheme.CertificateAuthentication;
                })
                .AddCertificateAuthentication(_container);

            services.AddMvc()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ContractResolver
                        = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
                });
            IntegrateSimpleInjector(services);
        }

        private void IntegrateSimpleInjector(IServiceCollection services)
        {
            _container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddSingleton<IControllerActivator>(
                new SimpleInjectorControllerActivator(_container));

            services.EnableSimpleInjectorCrossWiring(_container);
            services.UseSimpleInjectorAspNetRequestScoping(_container);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            var forwardingOptions = new ForwardedHeadersOptions()
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto |
                                   ForwardedHeaders.XForwardedHost
            };
            forwardingOptions.KnownNetworks.Clear();
            forwardingOptions.KnownProxies.Clear();
            app.UseForwardedHeaders(forwardingOptions);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(a =>
                {
                    a.Run(ctx =>
                    {
                        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        return Task.CompletedTask;
                    });
                });
            }

            //NLog config
            //loggerFactory.AddNLog();
            
            NLog.LogManager.LoadConfiguration("nlog.config");

            //SimpleInjector
            InitializeContainer(app);
            //register middleware

            _container.Verify();

            //app.Use((context, next) => _container.GetInstance<ErrorResponseMiddleware>().Invoke(context, next));
            app.Use((context, next) => _container.GetInstance<GlobalExceptionsLogger>().Invoke(context, next));

            //app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
            //definirano zbog IIS-a (inace middleware pipeline ne vraca 404); mora biti nakon UseMvcWithDefaultRoute
            app.Run(ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            });
        }

        private void InitializeContainer(IApplicationBuilder app)
        {
            // Add application presentation components:
            _container.RegisterMvcControllers(app);
            _container.RegisterMvcViewComponents(app);

            //settings
            var settings = Configuration.GetSection("Settings").Get<Settings>();
            _container.RegisterInstance(settings);
            var mqConnectionFactory = Configuration.GetSection("RabbitMqConnection").Get<ConnectionFactory>();
            _container.RegisterInstance(mqConnectionFactory);


            //bootstrapping
            _container.Register<MessageQueueService>(Lifestyle.Transient);
            RegisterMediatr(_container);
            

            // Cross-wire ASP.NET services (if any). For instance:
            _container.CrossWire<ILoggerFactory>(app);
            _container.CrossWire<IHttpContextAccessor>(app);
            // NOTE: Do prevent cross-wired instances as much as possible.
            // See: https://simpleinjector.org/blog/2016/07/
        }

        private void RegisterMediatr(Container container)
        {
            var assemblies = new[] { this.GetType().Assembly };
            container.RegisterSingleton<IMediator, Mediator>();
            container.Register(typeof(IRequestHandler<,>), assemblies);
            container.Register(typeof(IRequestHandler<>), assemblies);

            container.RegisterCollection(typeof(INotificationHandler<>),
                GetAllImplementations(container, typeof(INotificationHandler<>), assemblies));

            //mapping from specific to basic pipeline behaviour
            container.RegisterCollection(typeof(IPipelineBehavior<,>), new[]
            {
                typeof(LoggingBehavior<,>),
                typeof(RequestPreProcessorBehavior<,>),
                typeof(TransactionBehavior<,>),
                typeof(RequestPostProcessorBehavior<,>)
            });

            container.RegisterCollection(typeof(IRequestPreProcessor<>),
                GetAllImplementations(container, typeof(IRequestPreProcessor<>), assemblies));
            container.RegisterCollection(typeof(IRequestPostProcessor<,>),
                GetAllImplementations(container, typeof(IRequestPostProcessor<,>), assemblies));


            container.RegisterInstance(new SingleInstanceFactory(container.GetInstance));
            container.RegisterInstance(new MultiInstanceFactory(container.GetAllInstances));
        }

        private static IEnumerable<Type> GetAllImplementations(Container container, Type serviceType,
            Assembly[] assemblies)
        {
            return container.GetTypesToRegister(serviceType, assemblies,
                new TypesToRegisterOptions
                {
                    IncludeGenericTypeDefinitions = true,
                    IncludeComposites = false,
                });
        }
    }
}
