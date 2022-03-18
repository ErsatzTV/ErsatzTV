<template>
    <div>
        <v-data-table
            :headers="headers"
            :items="ffmpegProfiles"
            :sort-by="['name']"
            class="elevation-1"
        >
            <template v-slot:[`item.transcode`]="{ item }">
                <span>{{ item.transcode ? 'Yes' : 'No' }}</span>
            </template>
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
        { text: 'Name', value: 'name' },
        { text: 'Transcode', value: 'transcode' },
        { text: 'Resolution', value: 'resolution' },
        { text: 'Video', value: 'video' },
        { text: 'Audio', value: 'audio' }
    ];

    title: string = 'FFMpeg Profiles';

    async mounted(): Promise<void> {
        this.ffmpegProfiles = await ffmpegProfileApiService.getAll();
    }
}
</script>
