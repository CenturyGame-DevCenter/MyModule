﻿using System;

namespace CenturyGame.Framework.Base
{
    public class Singleton<TYPE>
    {
        public static TYPE Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (m_lockObject)
                    {
                        if (_instance == null)
                            _instance = (TYPE)Activator.CreateInstance(typeof(TYPE), true);
                    }
                }
                return _instance;
            }
        }

        private static TYPE _instance = default;
        private static readonly object m_lockObject = new object();
    }
}