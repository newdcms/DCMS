﻿using DCMS.Core.Infrastructure;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;

namespace DCMS.Web.Framework.Mvc.Routing
{

    public class RoutePublisher : IRoutePublisher
    {
        #region Fields

        /// <summary>
        /// Type finder
        /// </summary>
        protected readonly ITypeFinder _typeFinder;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="typeFinder">Type finder</param>
        public RoutePublisher(ITypeFinder typeFinder)
        {
            _typeFinder = typeFinder;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="endpointRouteBuilder">Route builder</param>
        public virtual void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            //find route providers provided by other assemblies
            var routeProviders = _typeFinder.FindClassesOfType<IRouteProvider>();

            //create and sort instances of route providers
            var instances = routeProviders
                .Select(routeProvider => (IRouteProvider)Activator.CreateInstance(routeProvider))
                .OrderByDescending(routeProvider => routeProvider.Priority);

            //register all provided routes
            foreach (var routeProvider in instances)
            {
                routeProvider.RegisterRoutes(endpointRouteBuilder);
            }
        }

        #endregion
    }

    //public class RoutePublisher : IRoutePublisher
    //{
    //    #region Fields

    //    /// <summary>
    //    /// Type finder
    //    /// </summary>
    //    protected readonly ITypeFinder _typeFinder;

    //    #endregion

    //    #region Ctor

    //    /// <summary>
    //    /// Ctor
    //    /// </summary>
    //    /// <param name="typeFinder">Type finder</param>
    //    public RoutePublisher(ITypeFinder typeFinder)
    //    {
    //        _typeFinder = typeFinder;
    //    }

    //    #endregion

    //    #region Methods

    //    /// <summary>
    //    /// Register routes
    //    /// </summary>
    //    /// <param name="routeBuilder">Route builder</param>
    //    public virtual void RegisterRoutes(IRouteBuilder routeBuilder)
    //    {
    //        //find route providers provided by other assemblies
    //        var routeProviders = _typeFinder.FindClassesOfType<IRouteProvider>();

    //        //create and sort instances of route providers
    //        var instances = routeProviders
    //            .Select(routeProvider => (IRouteProvider)Activator.CreateInstance(routeProvider))
    //            .OrderByDescending(routeProvider => routeProvider.Priority);

    //        //register all provided routes
    //        foreach (var routeProvider in instances)
    //        {
    //            routeProvider.RegisterRoutes(routeBuilder);
    //        }
    //    }

    //    #endregion
    //}
}
