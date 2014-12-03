///
/// <summary> * Bobo Browse Engine - High performance faceted/parametric search implementation 
/// * that handles various types of semi-structured data.  Written in Java.
/// * 
/// * Copyright (C) 2005-2006  John Wang
/// *
/// * This library is free software; you can redistribute it and/or
/// * modify it under the terms of the GNU Lesser General Public
/// * License as published by the Free Software Foundation; either
/// * version 2.1 of the License, or (at your option) any later version.
/// *
/// * This library is distributed in the hope that it will be useful,
/// * but WITHOUT ANY WARRANTY; without even the implied warranty of
/// * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
/// * Lesser General Public License for more details.
/// *
/// * You should have received a copy of the GNU Lesser General Public
/// * License along with this library; if not, write to the Free Software
/// * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
/// * 
/// * To contact the project administrators for the bobo-browse project, 
/// * please go to https://sourceforge.net/projects/bobo-browse/, or 
/// * send mail to owner@browseengine.com. </summary>
/// 

namespace BoboBrowse.Net.Fields
{
    using System;
    using System.Collections.Generic;
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Impl;

    public class FieldRegistry
	{
		private readonly Dictionary<string, System.Type> pluginMap;

		private FieldRegistry()
		{
			pluginMap = new Dictionary<string, System.Type>();
		}

		private static readonly FieldRegistry instance = new FieldRegistry();

		static FieldRegistry()
		{
            instance.RegisterFieldPlugin<PathFacetHandler>("path");
            instance.RegisterFieldPlugin<SimpleFacetHandler>("simple");
            instance.RegisterFieldPlugin<RangeFacetHandler>("range");
            instance.RegisterFieldPlugin<MultiValueFacetHandler>("tags");
            instance.RegisterFieldPlugin<MultiValueFacetHandler>("multi");
            instance.RegisterFieldPlugin<CompactMultiValueFacetHandler>("compact");
		}

		public static FieldRegistry GetInstance()
		{
			return instance;
		}

		public virtual System.Type GetFieldPlugin(string typename)// where JavaToDotNetGenericWildcard : FacetHandler
		{
			lock(pluginMap)
			{
				return pluginMap[typename];
			}
		}

		public virtual void RegisterFieldPlugin<T1>(string typename) where T1 : FacetHandler
		{
		    System.Type cls = typeof (T1);
			if (typename != null)
			{
				if (typeof(FacetHandler).IsAssignableFrom(cls))
				{
					lock (pluginMap)
					{
						string name = typename.Trim().ToLower();
						if (pluginMap[name] == null)
						{
							try
							{
							//plugin = (FieldPlugin) cls.newInstance();
								pluginMap.Add(name, cls);
							}
							catch (Exception e)
							{
								throw new ArgumentException(e.Message, e);
							}
						}
						else
						{
                            throw new ArgumentException("plugin: " + name + " already exists.");
						}
					}
				}
				else
				{
                    throw new ArgumentException(cls + " is not a valid subclass of " + typeof(FacetHandler));
				}
			}
		}
	}
}