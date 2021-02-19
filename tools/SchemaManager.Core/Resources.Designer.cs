﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SchemaManager.Core {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SchemaManager.Core.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to Completed applying Paas schema..
        /// </summary>
        internal static string ApplyPaasSchemaCompleted {
            get {
                return ResourceManager.GetString("ApplyPaasSchemaCompleted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Applying Paas schema for version: {0}..
        /// </summary>
        internal static string ApplyPaasSchemaStarted {
            get {
                return ResourceManager.GetString("ApplyPaasSchemaStarted", resourceCulture);
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
        ///   Looks up a localized string similar to The available versions are not up-to-date..
        /// </summary>
        internal static string AvailableVersionsErrorMessage {
            get {
                return ResourceManager.GetString("AvailableVersionsErrorMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Creating PaaSchemaVersion table if not exists..
        /// </summary>
        internal static string CreatePaasSchemaVersionTableMessage {
            get {
                return ResourceManager.GetString("CreatePaasSchemaVersionTableMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The schema version {0} cannot be applied because all the instances are not running the previous version..
        /// </summary>
        internal static string InvalidVersionMessage {
            get {
                return ResourceManager.GetString("InvalidVersionMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Paas schema already exists for version: {0}..
        /// </summary>
        internal static string PaasSchemaAlreadyExists {
            get {
                return ResourceManager.GetString("PaasSchemaAlreadyExists", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Script execution has failed due to &quot;{0}&quot;..
        /// </summary>
        internal static string QueryExecutionErrorMessage {
            get {
                return ResourceManager.GetString("QueryExecutionErrorMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to connect to host &quot;{0}&quot;..
        /// </summary>
        internal static string RequestFailedMessage {
            get {
                return ResourceManager.GetString("RequestFailedMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Attempt {0} of {1} to wait for the current version to be updated on the server..
        /// </summary>
        internal static string RetryCurrentSchemaVersion {
            get {
                return ResourceManager.GetString("RetryCurrentSchemaVersion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Attempt {0} of {1} to verify if all the instances are running the previous version..
        /// </summary>
        internal static string RetryCurrentVersions {
            get {
                return ResourceManager.GetString("RetryCurrentVersions", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Schema migration is started for the version : {0}..
        /// </summary>
        internal static string SchemaMigrationStartedMessage {
            get {
                return ResourceManager.GetString("SchemaMigrationStartedMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Schema migration is completed successfully for the version : {0}..
        /// </summary>
        internal static string SchemaMigrationSuccessMessage {
            get {
                return ResourceManager.GetString("SchemaMigrationSuccessMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The specified schema version &quot;{0}&quot; is not available..
        /// </summary>
        internal static string SpecifiedVersionNotAvailable {
            get {
                return ResourceManager.GetString("SpecifiedVersionNotAvailable", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The schema version &quot;{0}&quot; is not compatible..
        /// </summary>
        internal static string VersionIncompatibilityMessage {
            get {
                return ResourceManager.GetString("VersionIncompatibilityMessage", resourceCulture);
            }
        }
    }
}
