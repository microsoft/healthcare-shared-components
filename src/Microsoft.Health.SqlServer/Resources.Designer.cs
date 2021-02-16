﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.Health.SqlServer {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.Health.SqlServer.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There are no available versions..
        /// </summary>
        internal static string AvailableVersionsDefaultErrorMessage {
            get {
                return ResourceManager.GetString("AvailableVersionsDefaultErrorMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The base schema already exists..
        /// </summary>
        internal static string BaseSchemaAlreadyExists {
            get {
                return ResourceManager.GetString("BaseSchemaAlreadyExists", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The base schema execution is started..
        /// </summary>
        internal static string BaseSchemaExecuting {
            get {
                return ResourceManager.GetString("BaseSchemaExecuting", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The base schema execution is completed..
        /// </summary>
        internal static string BaseSchemaSuccess {
            get {
                return ResourceManager.GetString("BaseSchemaSuccess", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The base script is not found..
        /// </summary>
        internal static string BaseScriptNotFound {
            get {
                return ResourceManager.GetString("BaseScriptNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The compatible versions information is not available..
        /// </summary>
        internal static string CompatibilityDefaultErrorMessage {
            get {
                return ResourceManager.GetString("CompatibilityDefaultErrorMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The compatibility information was not found..
        /// </summary>
        internal static string CompatibilityRecordNotFound {
            get {
                return ResourceManager.GetString("CompatibilityRecordNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The current version information is not available due to &quot;{0}&quot;..
        /// </summary>
        internal static string CurrentDefaultErrorDescription {
            get {
                return ResourceManager.GetString("CurrentDefaultErrorDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not find stored procedure &apos;dbo.SelectCurrentSchemaVersion&apos;..
        /// </summary>
        internal static string CurrentSchemaVersionStoredProcedureNotFound {
            get {
                return ResourceManager.GetString("CurrentSchemaVersionStoredProcedureNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The current version information could not be fetched from the service. Please try again..
        /// </summary>
        internal static string InstanceSchemaRecordErrorMessage {
            get {
                return ResourceManager.GetString("InstanceSchemaRecordErrorMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Insufficient permissions to create the database..
        /// </summary>
        internal static string InsufficientDatabasePermissionsMessage {
            get {
                return ResourceManager.GetString("InsufficientDatabasePermissionsMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Insufficient permissions to create tables in the database..
        /// </summary>
        internal static string InsufficientTablesPermissionsMessage {
            get {
                return ResourceManager.GetString("InsufficientTablesPermissionsMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The SQL operation has failed..
        /// </summary>
        internal static string OperationFailed {
            get {
                return ResourceManager.GetString("OperationFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Precision value {0} must be greater than or equal to 1 and less than or equal to 53..
        /// </summary>
        internal static string PrecisionValueOutOfRange {
            get {
                return ResourceManager.GetString("PrecisionValueOutOfRange", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Attempt {0} of {1} to verify if the base schema is synced up with the service..
        /// </summary>
        internal static string RetryInstanceSchemaRecord {
            get {
                return ResourceManager.GetString("RetryInstanceSchemaRecord", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The provided version is unknown..
        /// </summary>
        internal static string ScriptNotFound {
            get {
                return ResourceManager.GetString("ScriptNotFound", resourceCulture);
            }
        }
    }
}
