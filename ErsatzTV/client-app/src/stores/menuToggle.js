import { defineStore } from 'pinia'

export const useMenuToggleStore = defineStore('menuToggle', {
    state: () => {
        return { miniMenu: false }
    },
    getters: {
        mini(state) {
            return state.miniMenu
        },
    },
    actions: {
        toggle() {
            this.miniMenu = !this.miniMenu
        },
    },
})