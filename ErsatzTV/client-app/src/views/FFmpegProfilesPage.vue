<template>
    <div>
        <v-btn color="success" class="ma-4" @click="addRecord()">{{
            $t('ffmpeg-profiles.add-profile')
        }}</v-btn>
        <v-data-table
            :headers="headers"
            :items="ffmpegProfiles"
            :sort-by="['name']"
            class="elevation-1"
        >
            <template v-slot:[`item.actions`]="{ item }">
                <v-btn icon class="mr-2" @click="editRow(item.id)">
                    <v-icon>mdi-lead-pencil</v-icon>
                </v-btn>

                <v-btn icon @click.stop="deleteRecord(item.id, item.name)">
                    <v-icon>mdi-delete</v-icon></v-btn
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
    public ffmpegProfiles: FFmpegProfile[] = [];

    get headers() {
        return [
            { text: this.$t('ffmpeg-profiles.table.name'), value: 'name' },
            {
                text: this.$t('ffmpeg-profiles.table.resolution'),
                value: 'resolution'
            },
            { text: this.$t('ffmpeg-profiles.table.video'), value: 'video' },
            { text: this.$t('ffmpeg-profiles.table.audio'), value: 'audio' },
            {
                text: this.$t('ffmpeg-profiles.actions'),
                value: 'actions',
                sortable: false
            }
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
                title: this.$t(
                    'ffmpeg-profiles.delete-dialog-title'
                ).toString(),
                // text: this.$t(
                //     'Delete "' + recordName + '" FFmpeg Profile?'
                // ).toString(),
                text: this.$t('ffmpeg-profiles.delete-dialog-text', {
                    profileName: '"' + recordName + '"'
                }).toString(),
                icon: 'warning',
                //iconColor: '#4CAF50',
                showCancelButton: true,
                cancelButtonText: this.$t('ffmpeg-profiles.no').toString(),
                confirmButtonText: this.$t('ffmpeg-profiles.yes').toString()
            })
            .then((result) => {
                if (result.isConfirmed) {
                    let index = this.ffmpegProfiles.findIndex(
                        (it) => it.id === record
                    );
                    this.ffmpegProfiles.splice(index, 1);
                    ffmpegProfileApiService.deleteRecord(String(record));
                    const Toast = this.$swal.mixin({
                        toast: true,
                        position: 'top-end',
                        showConfirmButton: false,
                        timer: 3000,
                        timerProgressBar: true,
                        didOpen: (toast) => {
                            toast.addEventListener(
                                'mouseenter',
                                this.$swal.stopTimer
                            );
                            toast.addEventListener(
                                'mouseleave',
                                this.$swal.resumeTimer
                            );
                        }
                    });

                    Toast.fire({
                        icon: 'success',
                        title: this.$t('ffmpeg-profiles.profile-deleted').toString()
                    });
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
