import Vue from "vue";
import VueRouter from "vue-router";
import HomePage from "../views/HomePage.vue";

Vue.use(VueRouter);

const routes = [
    {
        path: "/",
        name: "Home",
        component: HomePage,
        meta: {
            icon: "mdi-home",
            disabled: false
        }
    },
    {
        path: "/channels",
        name: "Channels",
        meta: {
            icon: "mdi-cog",
            disabled: true
        }
    },
    {
        path: "/ffmpeg-profiles",
        name: "FFmpeg Profiles",
        meta: {
            icon: "mdi-cog",
            disabled: true
        }
    },
    {
        path: "/watermarks",
        name: "Watermarks",
        meta: {
            icon: "mdi-cog",
            disabled: true
        }
    },
    {
        path: "/sources",
        name: "Media Sources",
        meta: {
            icon: "mdi-cog",
            disabled: false,
        },
        children: [
            {
                path: "/sources/local",
                name: "Local",
                meta: {
                    icon: "mdi-cog",
                    disabled: true
                }
            },
            {
                path: "/sources/emby",
                name: "Emby",
                meta: {
                    icon: "mdi-cog",
                    disabled: true
                }
            },
            {
                path: "/sources/jellyfin",
                name: "Jellyfin",
                meta: {
                    icon: "mdi-cog",
                    disabled: true
                }
            },
            {
                path: "/sources/plex",
                name: "Plex",
                meta: {
                    icon: "mdi-cog",
                    disabled: true
                }
            },
        ]
    },
    {
        path: "/media",
        name: "Media",
        meta: {
            icon: "mdi-cog",
            disabled: false,
        },
        children: [
            {
                path: "/media/libraries",
                name: "Libraries",
                meta: {
                    icon: "mdi-cog",
                    disabled: true
                }
            },
            {
                path: "/media/trash",
                name: "Trash",
                meta: {
                    icon: "mdi-cog",
                    disabled: true
                }
            },
            {
                path: "/media/tv-shows",
                name: "TV Shows",
                meta: {
                    icon: "mdi-cog",
                    disabled: true
                }
            },
            {
                path: "/media/movies",
                name: "Movies",
                meta: {
                    icon: "mdi-cog",
                    disabled: true
                }
            },
            {
                path: "/media/music-videos",
                name: "Music Videos",
                meta: {
                    icon: "mdi-cog",
                    disabled: true
                }
            },
            {
                path: "/media/other-videos",
                name: "Other Videos",
                meta: {
                    icon: "mdi-cog",
                    disabled: true
                }
            },
            {
                path: "/media/songs",
                name: "Songs",
                meta: {
                    icon: "mdi-cog",
                    disabled: true
                }
            },
        ]
    },
    {
        path: "/lists",
        name: "Lists",
        meta: {
            icon: "mdi-cog",
            disabled: false,
        },
        children: [
            {
                path: "/lists/collections",
                name: "Collections",
                meta: {
                    icon: "mdi-cog",
                    disabled: true
                }
            },
            {
                path: "/lists/trakt-lists",
                name: "Trakt Lists",
                meta: {
                    icon: "mdi-cog",
                    disabled: true
                }
            },
            {
                path: "/lists/filler-presets",
                name: "Filler Presets",
                meta: {
                    icon: "mdi-cog",
                    disabled: true
                }
            },
        ]
    },
    {
        path: "/schedules",
        name: "Schedules",
        meta: {
            icon: "mdi-cog",
            disabled: true
        }
    },
    {
        path: "/playouts",
        name: "Playouts",
        meta: {
            icon: "mdi-cog",
            disabled: true
        }
    },
    {
        path: "/settings",
        name: "Settings",
        meta: {
            icon: "mdi-cog",
            disabled: true
        }
    },
    {
        path: "/Logs",
        name: "Logs",
        meta: {
            icon: "mdi-card-text",
            disabled: true
        },
    },
];

const router = new VueRouter({
    mode: "history",
    base: process.env.BASE_URL,
    routes,
});

export default router;