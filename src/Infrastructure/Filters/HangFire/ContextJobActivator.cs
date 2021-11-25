﻿using System;
using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Application.Abstractions.Services.Identity;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;

namespace DN.WebApi.Infrastructure.Filters.HangFire
{
    public class ContextJobActivator : JobActivator
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ContextJobActivator(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        public override JobActivatorScope BeginScope(PerformContext context)
        {
            return new Scope(context, _scopeFactory.CreateScope());
        }

        private class Scope : JobActivatorScope, IServiceProvider
        {
            private readonly PerformContext _context;
            private readonly IServiceScope _scope;

            public Scope(PerformContext context, IServiceScope scope)
            {
                _context = context ?? throw new ArgumentNullException(nameof(context));
                _scope = scope ?? throw new ArgumentNullException(nameof(scope));

                setParametersScope();
            }

            private void setParametersScope()
            {
                ITenantService tenantService = _scope.ServiceProvider.GetRequiredService<ITenantService>();
                string tenant = _context.GetJobParameter<string>("tenant");
                tenantService.SetTenant(tenant);

                ICurrentUser currentUser = _scope.ServiceProvider.GetRequiredService<ICurrentUser>();
                string userId = _context.GetJobParameter<string>("userId");
                currentUser.SetUserJob(userId);
            }

            public override object Resolve(Type type)
            {
                return ActivatorUtilities.GetServiceOrCreateInstance(this, type);
            }

            object IServiceProvider.GetService(Type serviceType)
            {
                if (serviceType == typeof(PerformContext))
                    return _context;
                return _scope.ServiceProvider.GetService(serviceType);
            }
        }
    }
}