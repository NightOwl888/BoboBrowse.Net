/**
 * Bobo Browse Engine - High performance faceted/parametric search implementation 
 * that handles various types of semi-structured data.  Written in Java.
 * 
 * Copyright (C) 2005-2006  John Wang
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 * 
 * To contact the project administrators for the bobo-browse project, 
 * please go to https://sourceforge.net/projects/bobo-browse/, or 
 * send mail to owner@browseengine.com.
 */
namespace BoboBrowse.Net.Config
{
    using BoboBrowse.Net.Facets;
    using Common.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class FieldConfiguration
    {
        private IDictionary<string, FacetHandler> map;

        private static readonly ILog logger = LogManager.GetLogger<FieldConfiguration>();
        private static IDictionary<string, Type> VALUE_TYPE_MAP = new Dictionary<string, Type>()
        {
            { "integer", typeof(int) },
            { "char", typeof(char) },
            { "date", typeof(DateTime) },
            { "double", typeof(double) },
            { "float", typeof(float) },
            { "long", typeof(long) }
        };

        public FieldConfiguration()
        {
            map = new Dictionary<string, FacetHandler>();
        }

            // NOT IMPLEMENTED
            //public void addPlugin(String name,String type,Properties props){
            //  Class<? extends FacetHandler> cls=FieldRegistry.getInstance().getFieldPlugin(type);
            //  if (cls!=null){
            //    try
            //    {
            //      Constructor<? extends FacetHandler> c=null;
            //      FacetHandler plugin = null;
            //      if ("path".equals(type))
            //      {
            //        c = cls.getConstructor(String.class);  
            //        plugin=c.newInstance(name);
            //      }
            //      else if ("range".equals(type))
            //      {
            //        c = cls.getConstructor(String.class,boolean.class);  
            //        plugin=c.newInstance(name,true);
            //      }
            //      else
            //      {
            //        c = cls.getConstructor(String.class,TermListFactory.class);
            //        String valType=props==null ? "string" : props.getProperty("value_type");
            //        String formatString=props==null ? null : props.getProperty("format");
        	
            //        TermListFactory termFactory=null;
            //        if (valType!=null)
            //        {
            //            Class<?> supportedType=VALUE_TYPE_MAP.get(valType);
            //            if (supportedType!=null)
            //            {
            //                termFactory = new PredefinedTermListFactory(supportedType,formatString);
            //            }
            //        }
            //        plugin=c.newInstance(name,termFactory);
            //      }
            //      _map.put(name, plugin);
            //    }
            //    catch (Exception e)
            //    {
            //        e.printStackTrace();
            //      logger.error(e.getMessage(),e);
            //    }
            //  }
            //  else{
            //    logger.error(type+" not supported, skipped.");
            //  }
            //}

        public IEnumerable<FacetHandler> GetFacetHandlers()
        {
            return map.Values;
        }

        public bool FieldDefined(string fieldName)
        {
            return map.ContainsKey(fieldName);
        }

        public FacetHandler GetFieldPlugin(string fieldName)
        {
            if (map.ContainsKey(fieldName))
            {
                return map[fieldName];
            }

            return null;
        }

        public string[] GetFieldNames()
        {
            return map.Keys.ToArray();
        }

        public override string ToString()
        {
            return string.Join(";", map.Select(x => x.Key + "=" + x.Value).ToArray());
        }
    }
}
