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

namespace Castle.Facilities.Startable
{
	using System;
	using System.Reflection;

	using Castle.Core;

	using Castle.MicroKernel.LifecycleConcerns;


	public class StartConcern : ILifecycleConcern
	{
		private static readonly StartConcern instance = new StartConcern();

		protected StartConcern()
		{
		}

		public static StartConcern Instance
		{
			get { return instance; }
		}

		public void Apply(ComponentModel model, object component)
		{
			if (component is IStartable)
			{
				(component as IStartable).Start();
			}
			else if (model.Configuration != null)
			{
				String startMethod = model.Configuration.Attributes["startMethod"];

				if (startMethod != null)
				{
#if !NETCF
					MethodInfo method = model.Implementation.GetMethod(startMethod, Type.EmptyTypes);
#else
					MethodInfo method = model.Implementation.GetMethod(startMethod, new Type[0]);
#endif
					method.Invoke(component, null);
				}
			}
		}
	}
}
