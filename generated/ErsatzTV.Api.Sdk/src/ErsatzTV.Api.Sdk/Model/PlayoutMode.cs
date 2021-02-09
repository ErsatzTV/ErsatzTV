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
    /// Defines PlayoutMode
    /// </summary>
    
    [JsonConverter(typeof(StringEnumConverter))]
    
    public enum PlayoutMode
    {
        /// <summary>
        /// Enum Flood for value: Flood
        /// </summary>
        [EnumMember(Value = "Flood")]
        Flood = 1,

        /// <summary>
        /// Enum One for value: One
        /// </summary>
        [EnumMember(Value = "One")]
        One = 2,

        /// <summary>
        /// Enum Multiple for value: Multiple
        /// </summary>
        [EnumMember(Value = "Multiple")]
        Multiple = 3,

        /// <summary>
        /// Enum Duration for value: Duration
        /// </summary>
        [EnumMember(Value = "Duration")]
        Duration = 4

    }

}
