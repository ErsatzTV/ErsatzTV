import { defineStore } from "pinia";

const originURL = `${window.location.origin}`;

export const applicationState = defineStore("appState", {
    state: () => {
        return {
            miniMenu: false,
            currentVersion: "0.4.3-7cd2f9a-docker-nvidia", // Needs to be pulled from API with an action when ready
            m3uURL: originURL + "/iptv/channels.m3u", // this will need to be fixed for reverse proxies
            xmlURL: originURL + "/iptv/xmltv.xml", // this will need to be fixed for reverse proxies
            documentationURL: "https://ersatztv.org/",
            githubURL: "https://github.com/jasongdove/ErsatzTV",
            discordURL: "https://discord.gg/hHaJm3yGy6",
        };
    },
    getters: {
        isNavigationMini(state) {
            return state.miniMenu;
        },
        currentServerVersion(state) {
            return state.currentVersion;
        },
        navBarURLs(state) {
            return {
                m3uURL: state.m3uURL,
                xmlURL: state.xmlURL,
                documentationURL: state.documentationURL,
                githubURL: state.githubURL,
                discordURL: state.discordURL,
            };
        },
    },
    actions: {
        toggleMiniNavigation() {
            this.miniMenu = !this.miniMenu;
        },
        disableMiniNavigation() {
            this.miniMenu = false;
        },
    },
});
