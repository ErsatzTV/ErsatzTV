import { defineStore } from 'pinia'

export const snackbarState = defineStore('snackbarState', {
    state: () => {
        return {
            visible: false,
            message: ""
        }
    },
    getters: {
        snackbarStatus(state) {
            return {
                isVisible: state.visible,
                currentMessage: state.message
            }
        }
    },
    actions: {
        showSnackbar(message) {
            console.log(message)
            this.message = message
            this.visible = true
        },
        closeSnackbar() {
            this.visible = false;
        },
    },
})