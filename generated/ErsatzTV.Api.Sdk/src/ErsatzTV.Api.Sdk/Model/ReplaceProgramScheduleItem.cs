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
    /// ReplaceProgramScheduleItem
    /// </summary>
    [DataContract(Name = "ReplaceProgramScheduleItem")]
    public partial class ReplaceProgramScheduleItem : IEquatable<ReplaceProgramScheduleItem>, IValidatableObject
    {
        /// <summary>
        /// Gets or Sets StartType
        /// </summary>
        [DataMember(Name = "startType", EmitDefaultValue = false)]
        public StartType? StartType { get; set; }
        /// <summary>
        /// Gets or Sets PlayoutMode
        /// </summary>
        [DataMember(Name = "playoutMode", EmitDefaultValue = false)]
        public PlayoutMode? PlayoutMode { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="ReplaceProgramScheduleItem" /> class.
        /// </summary>
        /// <param name="index">index.</param>
        /// <param name="startType">startType.</param>
        /// <param name="startTime">startTime.</param>
        /// <param name="playoutMode">playoutMode.</param>
        /// <param name="mediaCollectionId">mediaCollectionId.</param>
        /// <param name="multipleCount">multipleCount.</param>
        /// <param name="playoutDuration">playoutDuration.</param>
        /// <param name="offlineTail">offlineTail.</param>
        public ReplaceProgramScheduleItem(int index = default(int), StartType? startType = default(StartType?), string startTime = default(string), PlayoutMode? playoutMode = default(PlayoutMode?), int mediaCollectionId = default(int), int? multipleCount = default(int?), string playoutDuration = default(string), bool? offlineTail = default(bool?))
        {
            this.Index = index;
            this.StartType = startType;
            this.StartTime = startTime;
            this.PlayoutMode = playoutMode;
            this.MediaCollectionId = mediaCollectionId;
            this.MultipleCount = multipleCount;
            this.PlayoutDuration = playoutDuration;
            this.OfflineTail = offlineTail;
        }

        /// <summary>
        /// Gets or Sets Index
        /// </summary>
        [DataMember(Name = "index", EmitDefaultValue = false)]
        public int Index { get; set; }

        /// <summary>
        /// Gets or Sets StartTime
        /// </summary>
        [DataMember(Name = "startTime", EmitDefaultValue = true)]
        public string StartTime { get; set; }

        /// <summary>
        /// Gets or Sets MediaCollectionId
        /// </summary>
        [DataMember(Name = "mediaCollectionId", EmitDefaultValue = false)]
        public int MediaCollectionId { get; set; }

        /// <summary>
        /// Gets or Sets MultipleCount
        /// </summary>
        [DataMember(Name = "multipleCount", EmitDefaultValue = true)]
        public int? MultipleCount { get; set; }

        /// <summary>
        /// Gets or Sets PlayoutDuration
        /// </summary>
        [DataMember(Name = "playoutDuration", EmitDefaultValue = true)]
        public string PlayoutDuration { get; set; }

        /// <summary>
        /// Gets or Sets OfflineTail
        /// </summary>
        [DataMember(Name = "offlineTail", EmitDefaultValue = true)]
        public bool? OfflineTail { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class ReplaceProgramScheduleItem {\n");
            sb.Append("  Index: ").Append(Index).Append("\n");
            sb.Append("  StartType: ").Append(StartType).Append("\n");
            sb.Append("  StartTime: ").Append(StartTime).Append("\n");
            sb.Append("  PlayoutMode: ").Append(PlayoutMode).Append("\n");
            sb.Append("  MediaCollectionId: ").Append(MediaCollectionId).Append("\n");
            sb.Append("  MultipleCount: ").Append(MultipleCount).Append("\n");
            sb.Append("  PlayoutDuration: ").Append(PlayoutDuration).Append("\n");
            sb.Append("  OfflineTail: ").Append(OfflineTail).Append("\n");
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
            return this.Equals(input as ReplaceProgramScheduleItem);
        }

        /// <summary>
        /// Returns true if ReplaceProgramScheduleItem instances are equal
        /// </summary>
        /// <param name="input">Instance of ReplaceProgramScheduleItem to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(ReplaceProgramScheduleItem input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Index == input.Index ||
                    this.Index.Equals(input.Index)
                ) && 
                (
                    this.StartType == input.StartType ||
                    this.StartType.Equals(input.StartType)
                ) && 
                (
                    this.StartTime == input.StartTime ||
                    (this.StartTime != null &&
                    this.StartTime.Equals(input.StartTime))
                ) && 
                (
                    this.PlayoutMode == input.PlayoutMode ||
                    this.PlayoutMode.Equals(input.PlayoutMode)
                ) && 
                (
                    this.MediaCollectionId == input.MediaCollectionId ||
                    this.MediaCollectionId.Equals(input.MediaCollectionId)
                ) && 
                (
                    this.MultipleCount == input.MultipleCount ||
                    (this.MultipleCount != null &&
                    this.MultipleCount.Equals(input.MultipleCount))
                ) && 
                (
                    this.PlayoutDuration == input.PlayoutDuration ||
                    (this.PlayoutDuration != null &&
                    this.PlayoutDuration.Equals(input.PlayoutDuration))
                ) && 
                (
                    this.OfflineTail == input.OfflineTail ||
                    (this.OfflineTail != null &&
                    this.OfflineTail.Equals(input.OfflineTail))
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
                hashCode = hashCode * 59 + this.Index.GetHashCode();
                hashCode = hashCode * 59 + this.StartType.GetHashCode();
                if (this.StartTime != null)
                    hashCode = hashCode * 59 + this.StartTime.GetHashCode();
                hashCode = hashCode * 59 + this.PlayoutMode.GetHashCode();
                hashCode = hashCode * 59 + this.MediaCollectionId.GetHashCode();
                if (this.MultipleCount != null)
                    hashCode = hashCode * 59 + this.MultipleCount.GetHashCode();
                if (this.PlayoutDuration != null)
                    hashCode = hashCode * 59 + this.PlayoutDuration.GetHashCode();
                if (this.OfflineTail != null)
                    hashCode = hashCode * 59 + this.OfflineTail.GetHashCode();
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
