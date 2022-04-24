import Vue from 'vue';
import App from './App.vue';
import vuetify from './plugins/vuetify';
import router from './router';
import { createPinia, PiniaVuePlugin } from 'pinia';
import autoPageTitleMixin from './mixins/autoPageTitle';
import 'roboto-fontface/css/roboto/roboto-fontface.css';
import '@mdi/font/css/materialdesignicons.css';
import i18n from './i18n';

Vue.config.productionTip = false;

Vue.use(PiniaVuePlugin);
const pinia = createPinia();

// Mixin to automate the page title when navigating... Will default to "ErsatzTV if no title value exported from page.
Vue.mixin(autoPageTitleMixin);

new Vue({
    vuetify,
    router,
    pinia,
    i18n,
    render: (h) => h(App)
}).$mount('#app');
