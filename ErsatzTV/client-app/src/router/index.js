import Vue from 'vue';
import VueRouter from 'vue-router';
import HomePage from '../views/HomePage.vue';
import ChannelsPage from '../views/ChannelsPage.vue';

Vue.use(VueRouter);

const routes = [
    {
        path: '/',
        name: 'home.title',
        component: HomePage,
        meta: {
            icon: 'mdi-home',
            disabled: false
        }
    },
    {
        path: '/channels',
        name: 'channels.title',
        component: ChannelsPage,
        meta: {
            icon: 'mdi-broadcast',
            disabled: false
        }
    },
    {
        path: '/ffmpeg-profiles',
        name: 'ffmpeg-profiles.title',
        meta: {
            icon: 'mdi-video-input-component',
            disabled: true
        }
    },
    {
        path: '/watermarks',
        name: 'watermarks.title',
        meta: {
            icon: 'mdi-watermark',
            disabled: true
        }
    },
    {
        path: '/sources',
        name: 'media-sources.title',
        meta: {
            icon: 'mdi-server-network',
            disabled: false
        },
        children: [
            {
                path: '/sources/local',
                name: 'media-sources.local.title',
                meta: {
                    icon: 'mdi-folder',
                    disabled: true
                }
            },
            {
                path: '/sources/emby',
                name: 'media-sources.emby.title',
                meta: {
                    icon: 'mdi-emby',
                    disabled: true
                }
            },
            {
                path: '/sources/jellyfin',
                name: 'media-sources.jellyfin.title',
                meta: {
                    icon: 'mdi-jellyfish',
                    disabled: true
                }
            },
            {
                path: '/sources/plex',
                name: 'media-sources.plex.title',
                meta: {
                    icon: 'mdi-plex',
                    disabled: true
                }
            }
        ]
    },
    {
        path: '/media',
        name: 'media.title',
        meta: {
            icon: 'mdi-cog',
            disabled: false
        },
        children: [
            {
                path: '/media/libraries',
                name: 'media.libraries.title',
                meta: {
                    icon: 'mdi-library',
                    disabled: true
                }
            },
            {
                path: '/media/trash',
                name: 'media.trash.title',
                meta: {
                    icon: 'mdi-trash-can',
                    disabled: true
                }
            },
            {
                path: '/media/tv-shows',
                name: 'media.tv-shows.title',
                meta: {
                    icon: 'mdi-television-classic',
                    disabled: true
                }
            },
            {
                path: '/media/movies',
                name: 'media.movies.title',
                meta: {
                    icon: 'mdi-movie',
                    disabled: true
                }
            },
            {
                path: '/media/music-videos',
                name: 'media.music-videos.title',
                meta: {
                    icon: 'mdi-music-circle',
                    disabled: true
                }
            },
            {
                path: '/media/other-videos',
                name: 'media.other-videos.title',
                meta: {
                    icon: 'mdi-video',
                    disabled: true
                }
            },
            {
                path: '/media/songs',
                name: 'media.songs.title',
                meta: {
                    icon: 'mdi-album',
                    disabled: true
                }
            }
        ]
    },
    {
        path: '/lists',
        name: 'lists.title',
        meta: {
            icon: 'mdi-format-list-bulleted',
            disabled: false
        },
        children: [
            {
                path: '/lists/collections',
                name: 'lists.collections.title',
                meta: {
                    icon: 'mdi-collage',
                    disabled: true
                }
            },
            {
                path: '/lists/trakt-lists',
                name: 'lists.trakt-lists.title',
                meta: {
                    icon: 'mdi-hammer',
                    disabled: true
                }
            },
            {
                path: '/lists/filler-presets',
                name: 'lists.filler-presets.title',
                meta: {
                    icon: 'mdi-tune-vertical',
                    disabled: true
                }
            }
        ]
    },
    {
        path: '/schedules',
        name: 'schedules.title',
        meta: {
            icon: 'mdi-calendar',
            disabled: true
        }
    },
    {
        path: '/playouts',
        name: 'playouts.title',
        meta: {
            icon: 'mdi-clipboard-play-multiple',
            disabled: true
        }
    },
    {
        path: '/settings',
        name: 'settings.title',
        meta: {
            icon: 'mdi-cog',
            disabled: true
        }
    },
    {
        path: '/Logs',
        name: 'logs.title',
        meta: {
            icon: 'mdi-card-text',
            disabled: true
        }
    }
];

const router = new VueRouter({
    mode: 'history',
    base: process.env.BASE_URL,
    routes
});

export default router;
