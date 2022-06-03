import Vue from 'vue';
import App from './App.vue';
import vuetify from './plugins/vuetify';
import router from './router';
import { createPinia, PiniaVuePlugin } from 'pinia';
import piniaPluginPersistedstate from 'pinia-plugin-persistedstate';
import autoPageTitleMixin from './mixins/autoPageTitle';
import 'roboto-fontface/css/roboto/roboto-fontface.css';
import '@mdi/font/css/materialdesignicons.css';
import i18n from './plugins/i18n';
import VueSweetalert2 from 'vue-sweetalert2';
import 'sweetalert2/dist/sweetalert2.min.css';
import './assets/css/global.scss';

Vue.config.productionTip = false;

Vue.use(PiniaVuePlugin);
const pinia = createPinia();
pinia.use(piniaPluginPersistedstate);
// Mixin to automate the page title when navigating... Will default to "ErsatzTV if no title value exported from page.
Vue.mixin(autoPageTitleMixin);

const options = {
    confirmButtonColor: '#1E1E1E',
    cancelButtonColor: '#1E1E1E',
    background: '#1E1E1E',
    iconColor: '#4CAF50'
};
Vue.use(VueSweetalert2, options);

new Vue({
    vuetify,
    router,
    pinia,
    i18n,
    render: (h) => h(App)
}).$mount('#app');
