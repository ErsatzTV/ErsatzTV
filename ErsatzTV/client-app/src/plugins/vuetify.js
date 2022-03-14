import Vue from 'vue';
import Vuetify from 'vuetify/lib/framework';
import colors from 'vuetify/lib/util/colors';

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
                success: colors.green
            }
        },
        options: {
            customProperties: true
        },
        dark: true
    }
});
