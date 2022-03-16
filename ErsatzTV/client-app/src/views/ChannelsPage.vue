<template>
    <div>
        <v-data-table
            :headers="headers"
            :items="channels"
            :sort-by="['number']"
            class="elevation-1"
        ></v-data-table>
    </div>
</template>

<script lang="ts">
import Vue from 'vue';
import { Component } from 'vue-property-decorator';
import { Channel } from '@/models/Channel';
import { channelApiService } from '@/services/ChannelService';

@Component({ components: {} })
export default class Channels extends Vue {
    private channels: Channel[] = [];

    private headers = [
        { text: 'Number', value: 'number' },
        { text: 'Logo', value: 'logo' },
        { text: 'Name', value: 'name' },
        { text: 'Language', value: 'language' },
        { text: 'Mode', value: 'streamingMode' },
        { text: 'FFmpeg Profile', value: 'ffmpegProfileId' }
    ];

    title(): string {
        return `Channels`;
    }

    async mounted(): Promise<void> {
        this.channels = await channelApiService.getAll();
    }
}
</script>
