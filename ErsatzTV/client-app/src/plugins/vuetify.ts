import Vue from 'vue';
import Vuetify from 'vuetify';
import colors from 'vuetify/lib/util/colors';
import VueI18n from './i18n';

Vue.use(Vuetify);

export default new Vuetify({
    icons: {
        iconfont: 'mdi' // default - only for display purposes
    },
    theme: {
        themes: {
            dark: {
                primary: '#c0c000',
                secondary: '#00c0c0',
                accent: colors.yellow.accent2,
                error: colors.red,
                warning: colors.orange,
                info: colors.lightBlue,
                success: colors.green,
                background: '#121212'
            }
        },
        options: {
            customProperties: true
        },
        dark: true
    },
    lang: {
        t: (key, ...params) => VueI18n.t(key, params).toString()
    }
});
