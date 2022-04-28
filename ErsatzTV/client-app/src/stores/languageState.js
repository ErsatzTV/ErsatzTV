import { defineStore } from 'pinia';
import VueI18n from '../plugins/i18n';

export const languageState = defineStore('languageState', {
    state: () => {
        return {
            language: 'en'
        };
    },
    getters: {
        currentLanguageCode: (state) => {
            return state.language;
        }
    },
    actions: {
        setLanguage(languageCode) {
            this.language = languageCode;
            VueI18n.locale = languageCode;
        }
    },
    persist: {
        afterRestore: (context) => {
            if (context.store.language) {
                VueI18n.locale = context.store.language;
            }
        }
    }
});
