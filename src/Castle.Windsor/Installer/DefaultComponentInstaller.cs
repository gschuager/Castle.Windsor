// Copyright 2004-2009 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


namespace Castle.Windsor.Installer
{
	using System.Linq;
	using System;
	using System.Collections.Generic;
	using Castle.Core.Configuration;
	using Castle.Core.Resource;
	using Castle.MicroKernel;
	using Castle.Windsor.Configuration.Interpreters;

	/// <summary>
	/// Default <see cref="IComponentsInstaller"/> implementation.
	/// </summary>
	public class DefaultComponentInstaller : IComponentsInstaller
	{
		#region IComponentsInstaller Members

		/// <summary>
		/// Perform installation.
		/// </summary>
		/// <param name="container">Target container</param>
		/// <param name="store">Configuration store</param>
		public void SetUp(IWindsorContainer container, IConfigurationStore store)
		{
			SetUpComponents(store.GetBootstrapComponents(), container);
			SetUpFacilities(store.GetFacilities(), container);
			SetUpComponents(store.GetComponents(), container);
#if !SILVERLIGHT && !NETCF
			SetUpChildContainers(store.GetConfigurationForChildContainers(), container);
#endif
		}

		#endregion

		protected virtual void SetUpFacilities(IConfiguration[] configurations, IWindsorContainer container)
		{
			foreach(IConfiguration facility in configurations)
			{
				string id = facility.Attributes["id"];
				string typeName = facility.Attributes["type"];
				if (string.IsNullOrEmpty(typeName)) continue;

				Type type = ObtainType(typeName);

				IFacility facilityInstance = InstantiateFacility(type);

#if DEBUG
				System.Diagnostics.Debug.Assert( id != null );
				System.Diagnostics.Debug.Assert( facilityInstance != null );
#endif
				container.AddFacility(id, facilityInstance);
			}
		}

		protected virtual void SetUpComponents(IConfiguration[] configurations, IWindsorContainer container)
		{
			foreach(IConfiguration component in configurations)
			{
				var id = component.Attributes["id"];
				
				var typeName = component.Attributes["type"];
				var serviceTypeName = component.Attributes["service"];
				
				if (string.IsNullOrEmpty(typeName)) continue;
				
				Type type = ObtainType(typeName);
				Type service = type;

				if (!string.IsNullOrEmpty(serviceTypeName))
				{
					service = ObtainType(serviceTypeName);
				}

				AssertImplementsService(id, service, type);

#if DEBUG
				System.Diagnostics.Debug.Assert( id != null );
				System.Diagnostics.Debug.Assert( type != null );
				System.Diagnostics.Debug.Assert( service != null );
#endif
				container.AddComponent(id, service, type);
				SetUpComponentForwardedTypes(container, component, typeName, id);

			}
		}

		private void AssertImplementsService(string id, Type service, Type type)
		{
			if (service.IsGenericTypeDefinition)
				type = type.MakeGenericType(service.GetGenericArguments());
			if (!service.IsAssignableFrom(type))
			{
				var message = string.Format("Could not set up component '{0}'. Type '{1}' does not implement service '{2}'", id,
				                            type.AssemblyQualifiedName, service.AssemblyQualifiedName);
				throw new Exception(message);
			}
		}

		private void SetUpComponentForwardedTypes(IWindsorContainer container, IConfiguration component, string typeName, string id)
		{
			var forwardedTypes = component.Children["forwardedTypes"];
			if (forwardedTypes == null) return;

			var forwarded = new List<Type>();
			foreach (var forwardedType in forwardedTypes.Children
				.Where(c => c.Name.Equals("add", StringComparison.InvariantCultureIgnoreCase)))
			{
				var forwardedServiceTypeName = forwardedType.Attributes["service"];
				try
				{
					forwarded.Add(ObtainType(forwardedServiceTypeName));
				}
				catch (Exception e)
				{
					throw new Exception(
						string.Format("Component {0}-{1} defines invalid forwarded type.", id ?? string.Empty, typeName), e);
				}
			}

			foreach (var forwadedType in forwarded)
			{
				container.Kernel.RegisterHandlerForwarding(forwadedType, id);
			}
		}

#if !SILVERLIGHT && !NETCF
		private static void SetUpChildContainers(IConfiguration[] configurations, IWindsorContainer parentContainer)
		{
			foreach(IConfiguration childContainerConfig in configurations)
			{
				string id = childContainerConfig.Attributes["name"];
				
				System.Diagnostics.Debug.Assert( id != null );

				new WindsorContainer(id, parentContainer, 
					new XmlInterpreter(new StaticContentResource(childContainerConfig.Value)));
			}
		}
#endif

		private static Type ObtainType(String typeName)
		{
			try
			{
				return Type.GetType(typeName, true, false);
			}
			catch (Exception e)
			{
				String message = String.Format("The type name {0} could not be located.", typeName);

				throw new Exception(message, e);
			}
		}

		private static IFacility InstantiateFacility(Type facilityType)
		{
			if (!typeof(IFacility).IsAssignableFrom( facilityType ))
			{
				String message = String.Format("Type {0} does not implement the interface IFacility.", facilityType.FullName);

				throw new Exception(message);
			}

			try
			{
				return (IFacility) Activator.CreateInstance(facilityType);
			}
			catch(Exception ex)
			{
				String message = String.Format("Could not instantiate {0}. Does it have a public default constructor?", facilityType.FullName);

				throw new Exception(message, ex);
			}
		}
	}
}
