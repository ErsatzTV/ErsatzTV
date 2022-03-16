import Vue from 'vue';
import VueRouter from 'vue-router';
import HomePage from '../views/HomePage.vue';
import ChannelsPage from '../views/ChannelsPage.vue';

Vue.use(VueRouter);

const routes = [
    {
        path: '/',
        name: 'Home',
        component: HomePage,
        meta: {
            icon: 'mdi-home',
            disabled: false
        }
    },
    {
        path: '/channels',
        name: 'Channels',
        component: ChannelsPage,
        meta: {
            icon: 'mdi-broadcast',
            disabled: false
        }
    },
    {
        path: '/ffmpeg-profiles',
        name: 'FFmpeg Profiles',
        meta: {
            icon: 'mdi-video-input-component',
            disabled: true
        }
    },
    {
        path: '/watermarks',
        name: 'Watermarks',
        meta: {
            icon: 'mdi-watermark',
            disabled: true
        }
    },
    {
        path: '/sources',
        name: 'Media Sources',
        meta: {
            icon: 'mdi-server-network',
            disabled: false
        },
        children: [
            {
                path: '/sources/local',
                name: 'Local',
                meta: {
                    icon: 'mdi-folder',
                    disabled: true
                }
            },
            {
                path: '/sources/emby',
                name: 'Emby',
                meta: {
                    icon: 'mdi-emby',
                    disabled: true
                }
            },
            {
                path: '/sources/jellyfin',
                name: 'Jellyfin',
                meta: {
                    icon: 'mdi-jellyfish',
                    disabled: true
                }
            },
            {
                path: '/sources/plex',
                name: 'Plex',
                meta: {
                    icon: 'mdi-plex',
                    disabled: true
                }
            }
        ]
    },
    {
        path: '/media',
        name: 'Media',
        meta: {
            icon: 'mdi-cog',
            disabled: false
        },
        children: [
            {
                path: '/media/libraries',
                name: 'Libraries',
                meta: {
                    icon: 'mdi-library',
                    disabled: true
                }
            },
            {
                path: '/media/trash',
                name: 'Trash',
                meta: {
                    icon: 'mdi-trash-can',
                    disabled: true
                }
            },
            {
                path: '/media/tv-shows',
                name: 'TV Shows',
                meta: {
                    icon: 'mdi-television-classic',
                    disabled: true
                }
            },
            {
                path: '/media/movies',
                name: 'Movies',
                meta: {
                    icon: 'mdi-movie',
                    disabled: true
                }
            },
            {
                path: '/media/music-videos',
                name: 'Music Videos',
                meta: {
                    icon: 'mdi-music-circle',
                    disabled: true
                }
            },
            {
                path: '/media/other-videos',
                name: 'Other Videos',
                meta: {
                    icon: 'mdi-video',
                    disabled: true
                }
            },
            {
                path: '/media/songs',
                name: 'Songs',
                meta: {
                    icon: 'mdi-album',
                    disabled: true
                }
            }
        ]
    },
    {
        path: '/lists',
        name: 'Lists',
        meta: {
            icon: 'mdi-format-list-bulleted',
            disabled: false
        },
        children: [
            {
                path: '/lists/collections',
                name: 'Collections',
                meta: {
                    icon: 'mdi-collage',
                    disabled: true
                }
            },
            {
                path: '/lists/trakt-lists',
                name: 'Trakt Lists',
                meta: {
                    icon: 'mdi-hammer',
                    disabled: true
                }
            },
            {
                path: '/lists/filler-presets',
                name: 'Filler Presets',
                meta: {
                    icon: 'mdi-tune-vertical',
                    disabled: true
                }
            }
        ]
    },
    {
        path: '/schedules',
        name: 'Schedules',
        meta: {
            icon: 'mdi-calendar',
            disabled: true
        }
    },
    {
        path: '/playouts',
        name: 'Playouts',
        meta: {
            icon: 'mdi-clipboard-play-multiple',
            disabled: true
        }
    },
    {
        path: '/settings',
        name: 'Settings',
        meta: {
            icon: 'mdi-cog',
            disabled: true
        }
    },
    {
        path: '/Logs',
        name: 'Logs',
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
