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
import { Vue, Component } from 'vue-property-decorator';
import { Channel } from '@/models/Channel';
import { channelApiService } from '@/services/ChannelService';

@Component
export default class Channels extends Vue {
    private channels: Channel[] = [];

    private headers = [
        { text: this.$t('channels.table.number'), value: 'number' },
        { text: this.$t('channels.table.logo'), value: 'logo' },
        { text: this.$t('channels.table.name'), value: 'name' },
        { text: this.$t('channels.table.language'), value: 'language' },
        { text: this.$t('channels.table.mode'), value: 'streamingMode' },
        {
            text: this.$t('channels.table.ffmpeg-profile'),
            value: 'ffmpegProfile'
        }
    ];

    title: string = 'Channels';

    async mounted(): Promise<void> {
        this.channels = await channelApiService.getAll();
    }
}
</script>
