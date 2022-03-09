import { defineStore } from 'pinia'

export const applicationState = defineStore('appState', {
    state: () => {
        return { 
            miniMenu: false,
            currentVersion: "0.4.3-7cd2f9a-docker-nvidia", // Needs to be pulled from API with an action when ready
            m3uURL: window.location.hostname + "iptv/channels.m3u",
            xmlURL: window.location.hostname + "iptv/xmltv.xml",
            documentationURL: "",
            githubURL: "",
            discordURL: "",
        }
    },
    getters: {
        isNavigationMini(state) {
            return state.miniMenu
        },
        currentServerVersion(state){
            return state.currentVersion
        }
    },
    actions: {
        toggleMiniNavigation() {
            this.miniMenu = !this.miniMenu
        },
        disableMiniNavigation() {
            this.miniMenu = false
        },
    },
})