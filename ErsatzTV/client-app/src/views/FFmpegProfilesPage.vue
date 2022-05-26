<template>
    <div>
        <v-btn color="success" class="ma-4" @click="addRecord()"
            >Add FFmpeg Profile</v-btn
        >
        <v-data-table
            :headers="headers"
            :items="ffmpegProfiles"
            :sort-by="['name']"
            class="elevation-1"
        >
            <template v-slot:[`item.actions`]="{ item }">
                <v-icon small class="mr-2" @click="editRow(item.id)">
                    mdi-lead-pencil
                </v-icon>
                <v-icon small @click.stop="deleteRecord(item.id, item.name)"
                    >mdi-delete</v-icon
                >
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

    private dialog = false;

    get headers() {
        return [
            { text: this.$t('ffmpeg-profiles.table.name'), value: 'name' },
            {
                text: this.$t('ffmpeg-profiles.table.resolution'),
                value: 'resolution'
            },
            { text: this.$t('ffmpeg-profiles.table.video'), value: 'video' },
            { text: this.$t('ffmpeg-profiles.table.audio'), value: 'audio' },
            { text: 'Actions', value: 'actions', sortable: false }
        ];
    }

    addRecord() {
        this.$router.push({
            name: 'add-ffmpeg-profile'
        });
    }

    deleteRecord(record: any, recordName: any) {
        this.$swal
            .fire({
                title: 'Are you sure?',
                text: 'Delete "' + recordName + '" FFmpeg Profile?',
                icon: 'warning',
                iconColor: '#4CAF50',
                showCancelButton: true,
                confirmButtonText: 'Yes'
            })
            .then((result) => {
                if (result.isConfirmed) {
                    let index = this.ffmpegProfiles.findIndex(
                        (it) => it.id === record
                    );
                    this.ffmpegProfiles.splice(index, 1);
                    ffmpegProfileApiService.deleteRecord(String(record));
                    this.$swal.fire({
                        html: '"' + recordName + '" FFmpeg Profile deleted.',
                        timer: 2200
                    });
                    this.$swal.fire(
                        'Deleted!',
                        '"' + recordName + '" FFmpeg Profile deleted.',
                        'success'
                    );
                }
            });
    }

    editRow(id: any) {
        this.$router.push({
            name: 'edit-ffmpeg',
            query: { id: id }
        });
    }

    title: string = 'FFMpeg Profiles';

    async mounted(): Promise<void> {
        this.ffmpegProfiles = await ffmpegProfileApiService.getAll();
    }
}
</script>
