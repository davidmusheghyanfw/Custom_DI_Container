using System;

namespace DI
{
    internal class DIRegistration
    {
        public Func<DIContainer, object> Factory { get; set; } // Factory method to create instances object> _transient;
        public bool IsSingleton { get; set; } // Determines if it’s a singleton{get;set;}
        public object Instance { get; set; } // Stores singleton instance (if applicable)tance{get;set;}
    }
    
}