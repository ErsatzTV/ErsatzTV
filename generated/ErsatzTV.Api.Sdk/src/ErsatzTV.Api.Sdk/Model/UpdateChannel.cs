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
    /// UpdateChannel
    /// </summary>
    [DataContract(Name = "UpdateChannel")]
    public partial class UpdateChannel : IEquatable<UpdateChannel>, IValidatableObject
    {
        /// <summary>
        /// Gets or Sets StreamingMode
        /// </summary>
        [DataMember(Name = "streamingMode", EmitDefaultValue = false)]
        public StreamingMode? StreamingMode { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateChannel" /> class.
        /// </summary>
        /// <param name="channelId">channelId.</param>
        /// <param name="name">name.</param>
        /// <param name="number">number.</param>
        /// <param name="ffmpegProfileId">ffmpegProfileId.</param>
        /// <param name="logo">logo.</param>
        /// <param name="streamingMode">streamingMode.</param>
        public UpdateChannel(int channelId = default(int), string name = default(string), int number = default(int), int ffmpegProfileId = default(int), string logo = default(string), StreamingMode? streamingMode = default(StreamingMode?))
        {
            this.ChannelId = channelId;
            this.Name = name;
            this.Number = number;
            this.FfmpegProfileId = ffmpegProfileId;
            this.Logo = logo;
            this.StreamingMode = streamingMode;
        }

        /// <summary>
        /// Gets or Sets ChannelId
        /// </summary>
        [DataMember(Name = "channelId", EmitDefaultValue = false)]
        public int ChannelId { get; set; }

        /// <summary>
        /// Gets or Sets Name
        /// </summary>
        [DataMember(Name = "name", EmitDefaultValue = true)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or Sets Number
        /// </summary>
        [DataMember(Name = "number", EmitDefaultValue = false)]
        public int Number { get; set; }

        /// <summary>
        /// Gets or Sets FfmpegProfileId
        /// </summary>
        [DataMember(Name = "ffmpegProfileId", EmitDefaultValue = false)]
        public int FfmpegProfileId { get; set; }

        /// <summary>
        /// Gets or Sets Logo
        /// </summary>
        [DataMember(Name = "logo", EmitDefaultValue = true)]
        public string Logo { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class UpdateChannel {\n");
            sb.Append("  ChannelId: ").Append(ChannelId).Append("\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
            sb.Append("  Number: ").Append(Number).Append("\n");
            sb.Append("  FfmpegProfileId: ").Append(FfmpegProfileId).Append("\n");
            sb.Append("  Logo: ").Append(Logo).Append("\n");
            sb.Append("  StreamingMode: ").Append(StreamingMode).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input)
        {
            return this.Equals(input as UpdateChannel);
        }

        /// <summary>
        /// Returns true if UpdateChannel instances are equal
        /// </summary>
        /// <param name="input">Instance of UpdateChannel to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(UpdateChannel input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.ChannelId == input.ChannelId ||
                    this.ChannelId.Equals(input.ChannelId)
                ) && 
                (
                    this.Name == input.Name ||
                    (this.Name != null &&
                    this.Name.Equals(input.Name))
                ) && 
                (
                    this.Number == input.Number ||
                    this.Number.Equals(input.Number)
                ) && 
                (
                    this.FfmpegProfileId == input.FfmpegProfileId ||
                    this.FfmpegProfileId.Equals(input.FfmpegProfileId)
                ) && 
                (
                    this.Logo == input.Logo ||
                    (this.Logo != null &&
                    this.Logo.Equals(input.Logo))
                ) && 
                (
                    this.StreamingMode == input.StreamingMode ||
                    this.StreamingMode.Equals(input.StreamingMode)
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 41;
                hashCode = hashCode * 59 + this.ChannelId.GetHashCode();
                if (this.Name != null)
                    hashCode = hashCode * 59 + this.Name.GetHashCode();
                hashCode = hashCode * 59 + this.Number.GetHashCode();
                hashCode = hashCode * 59 + this.FfmpegProfileId.GetHashCode();
                if (this.Logo != null)
                    hashCode = hashCode * 59 + this.Logo.GetHashCode();
                hashCode = hashCode * 59 + this.StreamingMode.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }

}
