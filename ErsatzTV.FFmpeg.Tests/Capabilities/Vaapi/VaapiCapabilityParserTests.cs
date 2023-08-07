using System.Collections.Generic;
using System.Linq;
using ErsatzTV.FFmpeg.Capabilities.Vaapi;
using FluentAssertions;
using NUnit.Framework;

namespace ErsatzTV.FFmpeg.Tests.Capabilities.Vaapi;

[TestFixture]
public class VaapiCapabilityParserTests
{
    private const string BriefOutput = @"Trying display: wayland
vainfo: VA-API version: 1.18 (libva 2.18.2)
vainfo: Driver version: Mesa Gallium driver 23.1.2 for AMD Radeon RX 6750 XT (navi22, LLVM 15.0.7, DRM 3.52, 6.3.8-arch1-1)
vainfo: Supported profile and entrypoints
      VAProfileMPEG2Simple            :	VAEntrypointVLD
      VAProfileMPEG2Main              :	VAEntrypointVLD
      VAProfileVC1Simple              :	VAEntrypointVLD
      VAProfileVC1Main                :	VAEntrypointVLD
      VAProfileVC1Advanced            :	VAEntrypointVLD
      VAProfileH264ConstrainedBaseline:	VAEntrypointVLD
      VAProfileH264ConstrainedBaseline:	VAEntrypointEncSlice
      VAProfileH264Main               :	VAEntrypointVLD
      VAProfileH264Main               :	VAEntrypointEncSlice
      VAProfileH264High               :	VAEntrypointVLD
      VAProfileH264High               :	VAEntrypointEncSlice
      VAProfileHEVCMain               :	VAEntrypointVLD
      VAProfileHEVCMain               :	VAEntrypointEncSlice
      VAProfileHEVCMain10             :	VAEntrypointVLD
      VAProfileHEVCMain10             :	VAEntrypointEncSlice
      VAProfileJPEGBaseline           :	VAEntrypointVLD
      VAProfileVP9Profile0            :	VAEntrypointVLD
      VAProfileVP9Profile2            :	VAEntrypointVLD
      VAProfileAV1Profile0            :	VAEntrypointVLD
      VAProfileNone                   :	VAEntrypointVideoProc";

    private const string FullOutput = @"Trying display: wayland
vainfo: VA-API version: 1.18 (libva 2.18.2)
vainfo: Driver version: Mesa Gallium driver 23.1.2 for AMD Radeon RX 6750 XT (navi22, LLVM 15.0.7, DRM 3.52, 6.3.8-arch1-1)
vainfo: Supported config attributes per profile/entrypoint pair
VAProfileMPEG2Simple/VAEntrypointVLD
    VAConfigAttribRTFormat                 : VA_RT_FORMAT_YUV420

VAProfileMPEG2Main/VAEntrypointVLD
    VAConfigAttribRTFormat                 : VA_RT_FORMAT_YUV420

VAProfileVC1Simple/VAEntrypointVLD
    VAConfigAttribRTFormat                 : VA_RT_FORMAT_YUV420

VAProfileVC1Main/VAEntrypointVLD
    VAConfigAttribRTFormat                 : VA_RT_FORMAT_YUV420

VAProfileVC1Advanced/VAEntrypointVLD
    VAConfigAttribRTFormat                 : VA_RT_FORMAT_YUV420

VAProfileH264ConstrainedBaseline/VAEntrypointVLD
    VAConfigAttribRTFormat                 : VA_RT_FORMAT_YUV420

VAProfileH264ConstrainedBaseline/VAEntrypointEncSlice
    VAConfigAttribRTFormat                 : VA_RT_FORMAT_YUV420
    VAConfigAttribRateControl              : VA_RC_CBR
                                             VA_RC_VBR
                                             VA_RC_CQP
    VAConfigAttribEncPackedHeaders         : VA_ENC_PACKED_HEADER_NONE
    VAConfigAttribEncMaxRefFrames          : l0=1
                                             l1=0
    VAConfigAttribEncMaxSlices             : 128
    VAConfigAttribEncSliceStructure        : VA_ENC_SLICE_STRUCTURE_POWER_OF_TWO_ROWS
                                             VA_ENC_SLICE_STRUCTURE_EQUAL_ROWS
    VAConfigAttribEncQualityRange          : number of supported quality levels is 32
    VAConfigAttribEncRateControlExt        : max_num_temporal_layers_minus1=3 temporal_layer_bitrate_control_flag=1
    VAConfigAttribMaxFrameSize             : max_frame_size=1
                                             multiple_pass=0

VAProfileH264Main/VAEntrypointVLD
    VAConfigAttribRTFormat                 : VA_RT_FORMAT_YUV420

VAProfileH264Main/VAEntrypointEncSlice
    VAConfigAttribRTFormat                 : VA_RT_FORMAT_YUV420
    VAConfigAttribRateControl              : VA_RC_CBR
                                             VA_RC_VBR
                                             VA_RC_CQP
    VAConfigAttribEncPackedHeaders         : VA_ENC_PACKED_HEADER_NONE
    VAConfigAttribEncMaxRefFrames          : l0=1
                                             l1=0
    VAConfigAttribEncMaxSlices             : 128
    VAConfigAttribEncSliceStructure        : VA_ENC_SLICE_STRUCTURE_POWER_OF_TWO_ROWS
                                             VA_ENC_SLICE_STRUCTURE_EQUAL_ROWS
    VAConfigAttribEncQualityRange          : number of supported quality levels is 32
    VAConfigAttribEncRateControlExt        : max_num_temporal_layers_minus1=3 temporal_layer_bitrate_control_flag=1
    VAConfigAttribMaxFrameSize             : max_frame_size=1
                                             multiple_pass=0

VAProfileH264High/VAEntrypointVLD
    VAConfigAttribRTFormat                 : VA_RT_FORMAT_YUV420

VAProfileH264High/VAEntrypointEncSlice
    VAConfigAttribRTFormat                 : VA_RT_FORMAT_YUV420
                                             VA_RT_FORMAT_YUV420_10
                                             VA_RT_FORMAT_YUV420_10BPP
    VAConfigAttribRateControl              : VA_RC_CBR
                                             VA_RC_VBR
                                             VA_RC_CQP
    VAConfigAttribEncPackedHeaders         : VA_ENC_PACKED_HEADER_NONE
    VAConfigAttribEncMaxRefFrames          : l0=1
                                             l1=0
    VAConfigAttribEncMaxSlices             : 128
    VAConfigAttribEncSliceStructure        : VA_ENC_SLICE_STRUCTURE_POWER_OF_TWO_ROWS
                                             VA_ENC_SLICE_STRUCTURE_EQUAL_ROWS
    VAConfigAttribEncQualityRange          : number of supported quality levels is 32
    VAConfigAttribEncRateControlExt        : max_num_temporal_layers_minus1=3 temporal_layer_bitrate_control_flag=1
    VAConfigAttribMaxFrameSize             : max_frame_size=1
                                             multiple_pass=0

VAProfileHEVCMain/VAEntrypointVLD
    VAConfigAttribRTFormat                 : VA_RT_FORMAT_YUV420

VAProfileHEVCMain/VAEntrypointEncSlice
    VAConfigAttribRTFormat                 : VA_RT_FORMAT_YUV420
    VAConfigAttribRateControl              : VA_RC_CBR
                                             VA_RC_VBR
                                             VA_RC_CQP
    VAConfigAttribEncPackedHeaders         : VA_ENC_PACKED_HEADER_SEQUENCE
    VAConfigAttribEncMaxRefFrames          : l0=1
                                             l1=0
    VAConfigAttribEncMaxSlices             : 128
    VAConfigAttribEncSliceStructure        : VA_ENC_SLICE_STRUCTURE_POWER_OF_TWO_ROWS
                                             VA_ENC_SLICE_STRUCTURE_EQUAL_ROWS
    VAConfigAttribEncQualityRange          : number of supported quality levels is 32
    VAConfigAttribMaxFrameSize             : max_frame_size=1
                                             multiple_pass=0

VAProfileHEVCMain10/VAEntrypointVLD
    VAConfigAttribRTFormat                 : VA_RT_FORMAT_YUV420
                                             VA_RT_FORMAT_YUV420_10
                                             VA_RT_FORMAT_YUV420_10BPP

VAProfileHEVCMain10/VAEntrypointEncSlice
    VAConfigAttribRTFormat                 : VA_RT_FORMAT_YUV420
                                             VA_RT_FORMAT_YUV420_10
                                             VA_RT_FORMAT_YUV420_10BPP
    VAConfigAttribRateControl              : VA_RC_CBR
                                             VA_RC_VBR
                                             VA_RC_CQP
    VAConfigAttribEncPackedHeaders         : VA_ENC_PACKED_HEADER_SEQUENCE
    VAConfigAttribEncMaxRefFrames          : l0=1
                                             l1=0
    VAConfigAttribEncMaxSlices             : 128
    VAConfigAttribEncSliceStructure        : VA_ENC_SLICE_STRUCTURE_POWER_OF_TWO_ROWS
                                             VA_ENC_SLICE_STRUCTURE_EQUAL_ROWS
    VAConfigAttribEncQualityRange          : number of supported quality levels is 32
    VAConfigAttribMaxFrameSize             : max_frame_size=1
                                             multiple_pass=0

VAProfileJPEGBaseline/VAEntrypointVLD
    VAConfigAttribRTFormat                 : VA_RT_FORMAT_YUV420
                                             VA_RT_FORMAT_YUV422
                                             VA_RT_FORMAT_YUV444
                                             VA_RT_FORMAT_YUV400

VAProfileVP9Profile0/VAEntrypointVLD
    VAConfigAttribRTFormat                 : VA_RT_FORMAT_YUV420

VAProfileVP9Profile2/VAEntrypointVLD
    VAConfigAttribRTFormat                 : VA_RT_FORMAT_YUV420_10
                                             VA_RT_FORMAT_YUV420_10BPP

VAProfileAV1Profile0/VAEntrypointVLD
    VAConfigAttribRTFormat                 : VA_RT_FORMAT_YUV420
                                             VA_RT_FORMAT_YUV420_10
                                             VA_RT_FORMAT_YUV420_10BPP

VAProfileNone/VAEntrypointVideoProc
    VAConfigAttribRTFormat                 : VA_RT_FORMAT_YUV420
                                             VA_RT_FORMAT_YUV422
                                             VA_RT_FORMAT_YUV444
                                             VA_RT_FORMAT_YUV400
                                             VA_RT_FORMAT_YUV420_10
                                             VA_RT_FORMAT_RGB32
                                             VA_RT_FORMAT_YUV420_10BPP";

    [Test]
    public void ShouldParseEntrypoints()
    {
        List<VaapiProfileEntrypoint> brief = VaapiCapabilityParser.Parse(BriefOutput);
        List<VaapiProfileEntrypoint> full = VaapiCapabilityParser.ParseFull(FullOutput);

        brief.Count.Should().Be(20);
        full.Count.Should().Be(20);
    }

    [Test]
    public void Full_ShouldParseRateControlModes()
    {
        List<VaapiProfileEntrypoint> full = VaapiCapabilityParser.ParseFull(FullOutput);

        full.Count.Should().Be(20);
        full.Count(e => e.VaapiEntrypoint.StartsWith("VAEntrypointEnc")).Should().BeGreaterThan(0);
        foreach (VaapiProfileEntrypoint entrypoint in full.Where(e => e.VaapiEntrypoint.StartsWith("VAEntrypointEnc")))
        {
            entrypoint.RateControlModes.Count.Should().BeGreaterThan(0);
        }
    }
}
