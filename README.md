# AspNetCoreMigrationShims

## Overview
Helpers &amp; compatibility shims for migrating existing legacy code bases to AspNetCore MVC.

This is very valuable if you are moving form a legacy code base that may be plagued with alot of old code and/or technical debt that can't be changed; but you still
need or want to move to .NET6 AspNetCore for all the other awesome benefits it provides.

The goal here is not to eliminate all possible migration but only to address the key elements that cannot be changed as part of the migration.  In general it's best
to migrate all your code to use the new features, new paradigm, new patterns.  And when you encounter issues that simply can't be changed but are blockers
(e.g. Json Payload structures sent by your Front-end or clients, etc.).

## Release Notes:
 - Provides NewtonsoftJson configuration & behavior for AspNetCore that closely matches the leagacy behavior of the legacy JsonMediaTypeFormatter of Asp .NET Framework MVC.

## NewtonsoftJson Input Formatter Compatibility
#### AspNetCoreMigrationShims.NewtonsoftJson.NetFrameworkCompatibility
 The new Newtonsoft Json Input Formatter has different behavior than it did in .NET Frameworks legacy MVC implementation. As noted above it's best to migrate to the new System.Text.Json
 for the inherent performance benefits and the better patterns now being enforced (e.g. fail-fast principle now better enforced by failing on invalid payloads).

 But, when migrating large legacy code bases from .NET Framework MVC to .NET 6 (AspNetCore) this is not always possible and the technical debt may have to live 
 on for a while longer, and this shouldn't be a blocker to migrate/upgrade.

 The two main differeces are as follows:
   1. The new behavior does not handle Null vs Default values the same way as legacy Newtonsoft Json did; this is now corrected by implementing the included `JsonNullToDefaultConverter`.
   2. The new behavior handles errors differently in most cases now throwing exceptions while also marking the model binding as unsuccessful; 
   whereby the legacy behavior of the `JsonMediaTypeFormatter` handled all errors while allowing the binding to be successful -- surfacing the errors *only* 
   in the `ModelState` that had to be manually inspected.
 
 This package incorporates the above into a new InputFormatter (`NetFrameworkMvcCompatibleNewtonsoftJsonInputFormatter`) and Configuration Extensions 
 (`AddNewtonsoftJsonWithNetFrameworkCompatibility` or `WithNewtonsoftJsonNetFrameworkCompatibility`) to wire everything up correctly. Thes configuration 
 methods are compatible with new Newtonsoft Configuration) but providing behavior in AspNetCore that closely matches the behavior of the legacy 
 JsonMediaTypeFormatter of Asp .NET Framework MVC.

 Example Usage:
 ```csharp
 // Configure MVC Services...
builder.Services
    .AddControllersWithViews(options =>
    {
        options.Filters.Add<MyCustomFilter>();
        //. . . etc . . .
    })
    //Here we add Newtonsoft.Json and can configure it just like the original AddNewtonsoftJson() configuration method allowed.
    .AddNewtonsoftJsonWithNetFrameworkCompatibility(options =>
    {
        //NOTE: Strongly recommended to always set the Default Memory Buffer appropriately for your use cases becasue the default is quite small...
        options.InputFormatterMemoryBufferThreshold = 1024 * 1024 * 5; //5MB
        
        //You can still customize the Json Serializer Settings just as you would...
        options.SerializerSettings.ContractResolver = new DefaultContractResolver()
        {
            //In my use case our Models have alot of technical debut and cannot be changed so we cannot enforce any specific Json naming convention!
            NamingStrategy = new DefaultNamingStrategy();
        };
    });
 ```
