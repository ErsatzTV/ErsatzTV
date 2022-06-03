<template>
    <div id="AddEditFFmpegProfile">
        <h1 id="title" class="mx-4" />
        <v-divider color="success" class="ma-2"></v-divider>
        <v-app>
            <v-form
                ref="form"
                v-model="isFormValid"
                id="ffmpegForm"
                lazy-validation
            >
                <v-container>
                    <v-row justify="center">
                        <v-flex shrink class="pb-10">
                            <v-container style="max-height: 80px">
                                <v-row justify="center">
                                    <h2 class="mx">
                                        {{ $t('edit-ffmpeg-profile.General') }}
                                    </h2>
                                </v-row>
                            </v-container>
                            <v-container style="max-height: 80px">
                                <v-text-field
                                    v-model="newProfile.name"
                                    :rules="textRules"
                                    :label="$t('edit-ffmpeg-profile.Name')"
                                    required
                                ></v-text-field>
                            </v-container>
                            <v-container
                                class="d-flex"
                                style="max-height: 80px"
                            >
                                <v-text-field
                                    ref="myThreadCount"
                                    v-model="threadCount"
                                    :label="
                                        $t('edit-ffmpeg-profile.thread-count')
                                    "
                                    :rules="validInt"
                                    number
                                    required
                                ></v-text-field>
                                <v-tooltip v-model="threadCountShow" top>
                                    <template v-slot:activator="{ on, attrs }">
                                        <v-icon
                                            class="pl-2"
                                            color="grey"
                                            v-ripple="false"
                                            :retain-focus="false"
                                            v-bind="attrs"
                                            v-on="on"
                                        >
                                            mdi-help-circle-outline
                                        </v-icon>
                                    </template>
                                    <span>Recommended Thread Count: 0</span>
                                </v-tooltip>
                            </v-container>
                            <v-container style="max-height: 40px">
                                <v-select
                                    v-model="selectedResolution"
                                    @change="preferredResolutionChange"
                                    :items="preferredResolutions"
                                    item-value="id"
                                    item-text="name"
                                    :label="
                                        $t(
                                            'edit-ffmpeg-profile.preferred-resolution'
                                        )
                                    "
                                    required
                                ></v-select>
                            </v-container>
                        </v-flex>
                        <v-divider
                            style="max-height: 500px"
                            vertical
                            color="grey"
                        ></v-divider>
                        <v-flex shrink class="pb-10">
                            <v-container style="max-height: 80px">
                                <v-row justify="center">
                                    <h2 class="mx">
                                        {{ $t('edit-ffmpeg-profile.Video') }}
                                    </h2>
                                </v-row>
                            </v-container>
                            <v-container
                                class="d-flex"
                                style="max-height: 80px"
                            >
                                <v-select
                                    v-model="selectedVideoFormat"
                                    @change="videoFormatChanged"
                                    :items="videoFormats"
                                    item-value="id"
                                    item-text="name"
                                    id="videoFormatSelector"
                                    :label="$t('edit-ffmpeg-profile.format')"
                                    required
                                ></v-select>
                                <v-tooltip v-model="videoFormatShow" top>
                                    <template v-slot:activator="{ on, attrs }">
                                        <v-icon
                                            class="pl-2"
                                            color="grey"
                                            v-ripple="false"
                                            :retain-focus="false"
                                            v-bind="attrs"
                                            v-on="on"
                                        >
                                            mdi-help-circle-outline
                                        </v-icon>
                                    </template>
                                    <span>Recommended Thread Count: 0</span>
                                </v-tooltip>
                            </v-container>
                            <v-container
                                class="d-flex"
                                style="max-height: 80px"
                            >
                                <v-text-field
                                    v-model="newProfile.videoBitRate"
                                    :label="$t('edit-ffmpeg-profile.bitrate')"
                                    :rules="validIntNonZero"
                                    required
                                ></v-text-field>
                                <v-tooltip v-model="videoBitRateShow" top>
                                    <template v-slot:activator="{ on, attrs }">
                                        <v-icon
                                            class="pl-2"
                                            color="grey"
                                            v-ripple="false"
                                            :retain-focus="false"
                                            v-bind="attrs"
                                            v-on="on"
                                        >
                                            mdi-help-circle-outline
                                        </v-icon>
                                    </template>
                                    <span>Recommended Thread Count: 0</span>
                                </v-tooltip>
                            </v-container>
                            <v-container
                                class="d-flex"
                                style="max-height: 80px"
                            >
                                <v-text-field
                                    v-model="newProfile.videoBufferSize"
                                    :label="
                                        $t('edit-ffmpeg-profile.buffer-size')
                                    "
                                    :rules="validIntNonZero"
                                    required
                                ></v-text-field>
                                <v-tooltip v-model="videoBufferSizeShow" top>
                                    <template v-slot:activator="{ on, attrs }">
                                        <v-icon
                                            class="pl-2"
                                            color="grey"
                                            v-ripple="false"
                                            :retain-focus="false"
                                            v-bind="attrs"
                                            v-on="on"
                                        >
                                            mdi-help-circle-outline
                                        </v-icon>
                                    </template>
                                    <span>Recommended Thread Count: 0</span>
                                </v-tooltip>
                            </v-container>
                            <v-container
                                class="d-flex"
                                style="max-height: 80px"
                            >
                                <v-select
                                    v-model="selectedHardwareAcceleration"
                                    @change="hardwareAccelerationChanged"
                                    :items="hardwareAccelerations"
                                    item-value="id"
                                    item-text="name"
                                    :label="
                                        $t(
                                            'edit-ffmpeg-profile.hardware-acceleration'
                                        )
                                    "
                                    required
                                ></v-select>
                                <v-tooltip
                                    v-model="hardwareAccelerationShow"
                                    top
                                >
                                    <template v-slot:activator="{ on, attrs }">
                                        <v-icon
                                            class="pl-2"
                                            color="grey"
                                            v-ripple="false"
                                            :retain-focus="false"
                                            v-bind="attrs"
                                            v-on="on"
                                        >
                                            mdi-help-circle-outline
                                        </v-icon>
                                    </template>
                                    <span>Recommended Thread Count: 0</span>
                                </v-tooltip>
                            </v-container>
                            <v-container
                                class="d-flex"
                                style="max-height: 80px"
                            >
                                <v-select
                                    v-model="selectedVaapiDriver"
                                    @change="vaapiDriverChanged"
                                    :items="vaapiDrivers"
                                    :disabled="vaapiDriverDisabled"
                                    item-value="id"
                                    item-text="name"
                                    :label="
                                        $t('edit-ffmpeg-profile.vaapi-driver')
                                    "
                                    required
                                ></v-select>
                                <v-tooltip v-model="vaapiDriverShow" top>
                                    <template v-slot:activator="{ on, attrs }">
                                        <v-icon
                                            class="pl-2"
                                            color="grey"
                                            v-ripple="false"
                                            :retain-focus="false"
                                            v-bind="attrs"
                                            v-on="on"
                                        >
                                            mdi-help-circle-outline
                                        </v-icon>
                                    </template>
                                    <span>Recommended Thread Count: 0</span>
                                </v-tooltip>
                            </v-container>
                            <v-container
                                class="d-flex"
                                style="max-height: 80px"
                            >
                                <v-select
                                    v-model="selectedVaapiDevice"
                                    @change="vaapiDeviceChanged"
                                    :items="vaapiDevices"
                                    :disabled="vaapiDriverDisabled"
                                    :label="
                                        $t('edit-ffmpeg-profile.vaapi-device')
                                    "
                                    required
                                ></v-select>
                                <v-tooltip v-model="vaapiDeviceShow" top>
                                    <template v-slot:activator="{ on, attrs }">
                                        <v-icon
                                            class="pl-2"
                                            color="grey"
                                            v-ripple="false"
                                            :retain-focus="false"
                                            v-bind="attrs"
                                            v-on="on"
                                        >
                                            mdi-help-circle-outline
                                        </v-icon>
                                    </template>
                                    <span>Recommended Thread Count: 0</span>
                                </v-tooltip>
                            </v-container>
                            <v-container
                                class="d-flex"
                                style="max-height: 50px"
                            >
                                <v-checkbox
                                    class="mr-2"
                                    v-model="normalizeFrameRate"
                                    :label="
                                        $t(
                                            'edit-ffmpeg-profile.normalize-framerate'
                                        )
                                    "
                                    color="green lighten-1"
                                    required
                                ></v-checkbox>
                                <v-tooltip v-model="normalizeFrameRateShow" top>
                                    <template v-slot:activator="{ on, attrs }">
                                        <v-icon
                                            class="pt-8"
                                            color="grey"
                                            v-ripple="false"
                                            :retain-focus="false"
                                            v-bind="attrs"
                                            v-on="on"
                                        >
                                            mdi-help-circle-outline
                                        </v-icon>
                                    </template>
                                    <span>Recommended Thread Count: 0</span>
                                </v-tooltip>
                            </v-container>
                            <v-container
                                class="d-flex"
                                style="max-height: 50px"
                            >
                                <v-checkbox
                                    class="mr-2"
                                    v-model="autoDeinterlaceVideo"
                                    :label="
                                        $t(
                                            'edit-ffmpeg-profile.auto-deinterlace-video'
                                        )
                                    "
                                    color="green lighten-1"
                                    required
                                ></v-checkbox>
                                <v-tooltip
                                    v-model="autoDeinterlaceVideoShow"
                                    top
                                >
                                    <template v-slot:activator="{ on, attrs }">
                                        <v-icon
                                            class="pt-8"
                                            color="grey"
                                            v-ripple="false"
                                            :retain-focus="false"
                                            v-bind="attrs"
                                            v-on="on"
                                        >
                                            mdi-help-circle-outline
                                        </v-icon>
                                    </template>
                                    <span>Recommended Thread Count: 0</span>
                                </v-tooltip>
                            </v-container>
                        </v-flex>
                        <v-divider
                            style="max-height: 500px"
                            vertical
                            color="grey"
                        ></v-divider>
                        <v-flex shrink class="pb-10">
                            <v-container style="max-height: 80px">
                                <v-row justify="center">
                                    <h2 class="mx">
                                        {{ $t('edit-ffmpeg-profile.Audio') }}
                                    </h2>
                                </v-row>
                            </v-container>
                            <v-container
                                class="d-flex"
                                style="max-height: 80px"
                            >
                                <v-select
                                    v-model="selectedAudioFormat"
                                    @change="audioFormatChanged"
                                    :items="audioFormats"
                                    item-value="id"
                                    item-text="name"
                                    :label="$t('edit-ffmpeg-profile.format')"
                                    ref="audioFormat"
                                    required
                                ></v-select>
                                <v-tooltip v-model="audioFormatShow" top>
                                    <template v-slot:activator="{ on, attrs }">
                                        <v-icon
                                            class="pl-2"
                                            color="grey"
                                            v-ripple="false"
                                            :retain-focus="false"
                                            v-bind="attrs"
                                            v-on="on"
                                        >
                                            mdi-help-circle-outline
                                        </v-icon>
                                    </template>
                                    <span>Recommended Thread Count: 0</span>
                                </v-tooltip>
                            </v-container>
                            <v-container
                                class="d-flex"
                                style="max-height: 80px"
                            >
                                <v-text-field
                                    v-model="newProfile.audioBitRate"
                                    :label="$t('edit-ffmpeg-profile.bitrate')"
                                    :rules="validIntNonZero"
                                    required
                                ></v-text-field>
                                <v-tooltip v-model="audioBitRateShow" top>
                                    <template v-slot:activator="{ on, attrs }">
                                        <v-icon
                                            class="pl-2"
                                            color="grey"
                                            v-ripple="false"
                                            :retain-focus="false"
                                            v-bind="attrs"
                                            v-on="on"
                                        >
                                            mdi-help-circle-outline
                                        </v-icon>
                                    </template>
                                    <span>Recommended Thread Count: 0</span>
                                </v-tooltip>
                            </v-container>
                            <v-container
                                class="d-flex"
                                style="max-height: 80px"
                            >
                                <v-text-field
                                    v-model="newProfile.audioBufferSize"
                                    :label="
                                        $t('edit-ffmpeg-profile.buffer-size')
                                    "
                                    :rules="validIntNonZero"
                                    required
                                ></v-text-field>
                                <v-tooltip v-model="audioBufferSizeShow" top>
                                    <template v-slot:activator="{ on, attrs }">
                                        <v-icon
                                            class="pl-2"
                                            color="grey"
                                            v-ripple="false"
                                            :retain-focus="false"
                                            v-bind="attrs"
                                            v-on="on"
                                        >
                                            mdi-help-circle-outline
                                        </v-icon>
                                    </template>
                                    <span>Recommended Thread Count: 0</span>
                                </v-tooltip>
                            </v-container>
                            <v-container
                                class="d-flex"
                                style="max-height: 80px"
                            >
                                <v-text-field
                                    v-model="newProfile.channels"
                                    :label="$t('edit-ffmpeg-profile.channels')"
                                    :rules="validIntNonZero"
                                    required
                                ></v-text-field>
                                <v-tooltip v-model="audioChannelsShow" top>
                                    <template v-slot:activator="{ on, attrs }">
                                        <v-icon
                                            class="pl-2"
                                            color="grey"
                                            v-ripple="false"
                                            :retain-focus="false"
                                            v-bind="attrs"
                                            v-on="on"
                                        >
                                            mdi-help-circle-outline
                                        </v-icon>
                                    </template>
                                    <span>Recommended Thread Count: 0</span>
                                </v-tooltip>
                            </v-container>
                            <v-container
                                class="d-flex"
                                style="max-height: 80px"
                            >
                                <v-text-field
                                    v-model="newProfile.audioSampleRate"
                                    :label="
                                        $t('edit-ffmpeg-profile.sample-rate')
                                    "
                                    :rules="validIntNonZero"
                                    required
                                ></v-text-field>
                                <v-tooltip v-model="audioSampleRateShow" top>
                                    <template v-slot:activator="{ on, attrs }">
                                        <v-icon
                                            class="pl-2"
                                            color="grey"
                                            v-ripple="false"
                                            :retain-focus="false"
                                            v-bind="attrs"
                                            v-on="on"
                                        >
                                            mdi-help-circle-outline
                                        </v-icon>
                                    </template>
                                    <span>Recommended Thread Count: 0</span>
                                </v-tooltip>
                            </v-container>
                            <v-container
                                class="d-flex"
                                style="max-height: 80px"
                            >
                                <v-checkbox
                                    v-model="normalizeLoudness"
                                    :label="
                                        $t(
                                            'edit-ffmpeg-profile.normalize-loudness'
                                        )
                                    "
                                    color="green lighten-1"
                                    required
                                ></v-checkbox>
                                <v-tooltip v-model="normalizeLoudnessShow" top>
                                    <template v-slot:activator="{ on, attrs }">
                                        <v-icon
                                            class="pl-2"
                                            color="grey"
                                            v-ripple="false"
                                            :retain-focus="false"
                                            v-bind="attrs"
                                            v-on="on"
                                        >
                                            mdi-help-circle-outline
                                        </v-icon>
                                    </template>
                                    <span>Recommended Thread Count: 0</span>
                                </v-tooltip>
                            </v-container>
                            <v-spacer style="height: 80px"></v-spacer>
                            <v-container>
                                <v-btn
                                    color="green lighten-1"
                                    class="ma-2"
                                    :disabled="!isFormValid"
                                    @click="saveFFmpegProfile()"
                                >
                                    {{ $t('edit-ffmpeg-profile.save-profile') }}
                                </v-btn>
                                <v-btn
                                    color="cancel"
                                    class="ma-2"
                                    @click="cancelAdd()"
                                >
                                    {{ $t('edit-ffmpeg-profile.cancel') }}
                                </v-btn>
                                <v-btn
                                    color="indigo accent-1"
                                    class="ma-2"
                                    @click="cancelAdd()"
                                >
                                    {{ $t('edit-ffmpeg-profile.help') }}
                                </v-btn>
                            </v-container>
                        </v-flex>
                    </v-row>
                </v-container>
            </v-form>
        </v-app>
    </div>
</template>

<script lang="ts">
import { Vue, Component, Watch } from 'vue-property-decorator';
import { ffmpegProfileApiService } from '@/services/FFmpegProfileService';

@Component
export default class AddEditFFmpegProfile extends Vue {
    //@Name({ default: 'AddEditFFmpegProfile' }) AddEditFFmpegProfile!: string;
    //@Prop({ default: -1 }) private id!: number;
    public newProfile: any = {};
    private refForm: any = this.$refs.form;
    public isFormValid = false;
    public threadCountShow = false;
    public videoFormatShow = false;
    public videoBitRateShow = false;
    public videoBufferSizeShow = false;
    public hardwareAccelerationShow = false;
    public vaapiDriverShow = false;
    public vaapiDeviceShow = false;
    public normalizeFrameRateShow = false;
    public autoDeinterlaceVideoShow = false;
    public audioFormatShow = false;
    public audioBitRateShow = false;
    public audioBufferSizeShow = false;
    public audioChannelsShow = false;
    public audioSampleRateShow = false;
    public normalizeLoudnessShow = false;

    public AddEditFFmpegProfile() {
        console.log('test');
    }

    public audioFormats: [
        { id: number; name: string },
        { id: number; name: string }
    ] = [
        { id: 1, name: 'aac' },
        { id: 2, name: 'ac3' }
    ];

    private _selectedAudioFormat: number = 2;
    public selectedAudioFormat: { id: number; name: string } = {
        id: 2,
        name: 'ac3'
    };

    public preferredResolutions: [
        { id: number; name: string },
        { id: number; name: string },
        { id: number; name: string },
        { id: number; name: string }
    ] = [
        { id: 0, name: '720x480' },
        { id: 1, name: '1280x720' },
        { id: 2, name: '1920x1080' },
        { id: 3, name: '3840x2160' }
    ];

    private _selectedResolution: number = 2;
    public selectedResolution: { id: number; name: string } = {
        id: 2,
        name: '1920x1080'
    };

    public videoFormats: [
        { id: number; name: string },
        { id: number; name: string },
        { id: number; name: string }
    ] = [
        { id: 1, name: 'h264' },
        { id: 2, name: 'hevc' },
        { id: 3, name: 'mpeg-2' }
    ];

    private _selectedVideoFormat: number = 1;
    public selectedVideoFormat: { id: number; name: string } = {
        id: 1,
        name: 'h264'
    };

    public hardwareAccelerations: [
        { id: number; name: string },
        { id: number; name: string },
        { id: number; name: string },
        { id: number; name: string },
        { id: number; name: string }
    ] = [
        { id: 0, name: 'None' },
        { id: 1, name: 'Qsv' },
        { id: 2, name: 'Nvenc' },
        { id: 3, name: 'Vaapi' },
        { id: 4, name: 'VideoToolbox' }
    ];

    private _selectedHardwareAcceleration: number = 0;
    public selectedHardwareAcceleration: { id: number; name: string } = {
        id: 0,
        name: 'None'
    };

    public vaapiDrivers: [
        { id: number; name: string },
        { id: number; name: string },
        { id: number; name: string },
        { id: number; name: string },
        { id: number; name: string }
    ] = [
        { id: 0, name: 'Default' },
        { id: 1, name: 'iHD' },
        { id: 2, name: 'i965' },
        { id: 3, name: 'RadeonSI' },
        { id: 4, name: 'Nouveau' }
    ];

    public vaapiDriverDisabled = true;
    private _selectedVaapiDriver: number = 0;
    public selectedVaapiDriver: { id: number; name: string } = {
        id: 0,
        name: 'Default'
    };

    public selectedVaapiDevice: string = '';
    public vaapiDevices: [string, string] = ['', '/dev/dri/renderD128'];

    public normalizeFrameRate: boolean = false;
    public autoDeinterlaceVideo: boolean = true;
    public normalizeLoudness: boolean = true;

    saveFFmpegProfile() {
        //this means we're adding
        if (isNaN(this.id)) {
            ffmpegProfileApiService.newFFmpegProfile(
                this.newProfile.name,
                this.threadCount,
                this._selectedHardwareAcceleration,
                this._selectedVaapiDriver,
                this.selectedVaapiDevice,
                this._selectedResolution,
                this._selectedVideoFormat,
                this.newProfile.videoBitRate,
                this.newProfile.videoBufferSize,
                this._selectedAudioFormat,
                this.newProfile.audioBitRate,
                this.newProfile.audioBufferSize,
                this.normalizeLoudness,
                this.newProfile.channels,
                this.newProfile.audioSampleRate,
                this.normalizeFrameRate,
                this.autoDeinterlaceVideo
            );
        } else {
            //this means we're editing
            ffmpegProfileApiService.updateFFmpegProfile(
                this.id,
                this.newProfile.name,
                this.threadCount,
                this._selectedHardwareAcceleration,
                this._selectedVaapiDriver,
                this.selectedVaapiDevice,
                this._selectedResolution,
                this._selectedVideoFormat,
                this.newProfile.videoBitRate,
                this.newProfile.videoBufferSize,
                this._selectedAudioFormat,
                this.newProfile.audioBitRate,
                this.newProfile.audioBufferSize,
                this.normalizeLoudness,
                this.newProfile.channels,
                this.newProfile.audioSampleRate,
                this.normalizeFrameRate,
                this.autoDeinterlaceVideo
            );
        }
        const Toast = this.$swal.mixin({
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 3000,
            timerProgressBar: true,
            didOpen: (toast) => {
                toast.addEventListener('mouseenter', this.$swal.stopTimer);
                toast.addEventListener('mouseleave', this.$swal.resumeTimer);
            }
        });

        Toast.fire({
            icon: 'success',
            title: this.$t('edit-ffmpeg-profile.profile-saved').toString()
        });
        this.$router.push({
            name: 'ffmpeg-profiles.title'
        });
    }

    cancelAdd() {
        this.$router.push({
            name: 'ffmpeg-profiles.title'
        });
    }

    //~change events~//
    public audioFormatChanged(selectObj: number) {
        this._selectedAudioFormat = selectObj;
    }

    public preferredResolutionChange(selectObj: number) {
        this._selectedResolution = selectObj + 1;
    }

    public videoFormatChanged(selectObj: number) {
        this._selectedVideoFormat = selectObj;
    }

    public hardwareAccelerationChanged(selectObj: number) {
        this._selectedHardwareAcceleration = selectObj;
        this.applyVaapiValidation();
    }

    public applyVaapiValidation() {
        //If they pick VAAPI as the hardware acceleration,
        //they can now choose a vaapi driver and device.
        //If not, they cannot change the default options.
        if (this._selectedHardwareAcceleration == 3) {
            this.vaapiDriverDisabled = false;
        } else {
            this.vaapiDriverDisabled = true;
            this._selectedVaapiDriver = 0;
            this.selectedVaapiDriver = { id: 0, name: 'Default' };
            this.selectedVaapiDevice = '';
        }
    }

    public vaapiDriverChanged(selectObj: number) {
        this._selectedVaapiDriver = selectObj;
    }

    public vaapiDeviceChanged(selectObj: string) {
        this.selectedVaapiDevice = selectObj;
    }

    //~ end change events~//

    public threadCount = 0;

    get validIntNonZero() {
        return [
            (v: any) =>
                (v && /^[0-9]+$/.test(v)) || this.$t('Must be a valid number.'),
            (v: any) => (v && v > 0) || 'Must be greater than 0.'
        ];
    }

    get validInt() {
        return [
            (v: any) =>
                (v && /^[0-9]+$/.test(v)) || this.$t('Must be a valid number.')
        ];
    }

    get textRules() {
        return [(v: any) => (v && v.length > 0) || 'Value must not be empty.'];
    }

    props!: { id: number };
    @Watch('id', { immediate: true }) async onItemChanged() {
        console.log('ID', this.id);
        this.id = Number(this.$route.query.id) ?? -1;
        await this.loadPage();
    }

    private loaded = false;
    private id = -1;

    title: string = 'Modify FFmpeg Profile';

    async loadPage(): Promise<void> {
        if (this.loaded) {
            return;
        }
        var title = document.getElementById('title');
        if (title === null || title === undefined) {
            //sometimes the element isn't loaded yet, it'll come
            //back to this when it's good to go. So skip for now.
            return;
        }
        if (!isNaN(this.id)) {
            title.innerHTML = this.$t(
                'edit-ffmpeg-profile.edit-profile'
            ).toString();
            var ffmpegFullProfile = await ffmpegProfileApiService.getOne(
                this.id.toString()
            );
            var result = ffmpegFullProfile[0];
            if (result !== undefined) {
                //We have a profile, let's load it.
                this.threadCount = result.threadCount;
                this.selectedVaapiDevice = result.vaapiDevice;
                this.autoDeinterlaceVideo = result.deinterlaceVideo;
                this.normalizeFrameRate = result.normalizeFramerate;
                this.normalizeLoudness = result.normalizeLoudness;
                this.newProfile = {
                    name: result.name,
                    videoBitRate: result.videoBitrate,
                    videoBufferSize: result.videoBufferSize,
                    audioBitRate: result.audioBitrate,
                    audioBufferSize: result.audioBufferSize,
                    channels: result.audioChannels,
                    audioSampleRate: result.audioSampleRate
                };

                this._selectedAudioFormat = result.audioFormat;
                this.selectedAudioFormat =
                    this.audioFormats[result.audioFormat - 1];
                this._selectedVideoFormat = result.videoFormat;
                this.selectedVideoFormat =
                    this.videoFormats[result.videoFormat - 1];
                this._selectedResolution = result.resolutionId;
                this.selectedResolution =
                    this.preferredResolutions[result.resolutionId - 1];
                this._selectedHardwareAcceleration =
                    result.hardwareAcceleration;
                this.selectedHardwareAcceleration =
                    this.hardwareAccelerations[result.hardwareAcceleration];
                this._selectedVaapiDriver = result.vaapiDriver;
                this.selectedVaapiDriver =
                    this.vaapiDrivers[result.vaapiDriver];
                this.applyVaapiValidation();
                this.loaded = true;
            } else {
                //an ID was entered (probably in the URL) that doesn't exist. Let's returnt to the profile list.
                console.log('No ffmpeg profile found for ID: ' + this.id);
                this.$router.push({
                    name: 'ffmpeg-profiles.title'
                });
            }
        } else {
            //new profile!
            title.innerHTML = this.$t(
                'edit-ffmpeg-profile.add-profile'
            ).toString();
            this._selectedAudioFormat = 2;
            this._selectedResolution = 3;
            this._selectedVideoFormat = 1;
            this._selectedHardwareAcceleration = 0;
            this._selectedVaapiDriver = 0;
            this.selectedVaapiDevice = '';
            this.newProfile = {
                name: 'New Profile',
                videoBitRate: 2000,
                videoBufferSize: 4000,
                audioBitRate: 192,
                audioBufferSize: 384,
                channels: 2,
                audioSampleRate: 48
            };
            this.loaded = true;
        }
    }
}
</script>
