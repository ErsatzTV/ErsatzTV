<template>
    <v-select
        v-on:change="setUserLanguage($event)"
        :value="language"
        dense
        outlined
        :items="langs"
        :label="$t('sidebar.languages')"
        class="ma-2"
    ></v-select>
</template>

<script>
import { mapState } from 'pinia';
import { languageState } from '@/stores/languageState';

export default {
    name: 'language-changer',
    data() {
        return {
            langs: [
                { text: this.$t('languages-code.en'), value: 'en' },
                { text: this.$t('languages-code.pt-br'), value: 'pt-br' }
            ]
        };
    },
    computed: {
        ...mapState(languageState, ['language', 'setLanguage']),
        languageCode: {
            get() {
                return languageState.language;
            },
            set(value) {
                this.setLanguage(value);
            }
        }
    },
    methods: {
        setUserLanguage: ($event) => {
            languageState().setLanguage($event);
        }
    }
};
</script>
