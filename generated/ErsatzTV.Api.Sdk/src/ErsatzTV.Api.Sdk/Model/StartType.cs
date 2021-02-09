/*
 * ErsatzTV API
 *
 * No description provided (generated by Openapi Generator https://github.com/openapitools/openapi-generator)
 *
 * The version of the OpenAPI document: v1
 * Generated by: https://github.com/openapitools/openapi-generator.git
 */


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using OpenAPIDateConverter = ErsatzTV.Api.Sdk.Client.OpenAPIDateConverter;

namespace ErsatzTV.Api.Sdk.Model
{
    /// <summary>
    /// Defines StartType
    /// </summary>
    
    [JsonConverter(typeof(StringEnumConverter))]
    
    public enum StartType
    {
        /// <summary>
        /// Enum Dynamic for value: Dynamic
        /// </summary>
        [EnumMember(Value = "Dynamic")]
        Dynamic = 1,

        /// <summary>
        /// Enum Fixed for value: Fixed
        /// </summary>
        [EnumMember(Value = "Fixed")]
        Fixed = 2

    }

}
