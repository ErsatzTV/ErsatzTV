<template>
    <div>
        <v-data-table
            :headers="headers"
            :items="ffmpegProfiles"
            :sort-by="['name']"
            class="elevation-1"
        >
        </v-data-table>
    </div>
</template>

<script lang="ts">
import { Vue, Component } from 'vue-property-decorator';
import { FFmpegProfile } from '@/models/FFmpegProfile';
import { ffmpegProfileApiService } from '@/services/FFmpegProfileService';

@Component
export default class FFmpegProfiles extends Vue {
    private ffmpegProfiles: FFmpegProfile[] = [];

    private headers = [
        { text: this.$t('ffmpeg-profiles.table.name'), value: 'name' },
        {
            text: this.$t('ffmpeg-profiles.table.resolution'),
            value: 'resolution'
        },
        { text: this.$t('ffmpeg-profiles.table.video'), value: 'video' },
        { text: this.$t('ffmpeg-profiles.table.audio'), value: 'audio' }
    ];

    title: string = 'FFMpeg Profiles';

    async mounted(): Promise<void> {
        this.ffmpegProfiles = await ffmpegProfileApiService.getAll();
    }
}
</script>
