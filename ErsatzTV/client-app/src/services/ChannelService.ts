import { AbstractApiService } from './AbstractApiService';
import { Channel } from '@/models/Channel';

class ChannelApiService extends AbstractApiService {
    public constructor() {
        super();
    }

    public getAll(): Promise<Channel[]> {
        return this.http
            .get('/api/channels')
            .then(this.handleResponse.bind(this))
            .catch(this.handleError.bind(this));
    }
}

export const channelApiService: ChannelApiService = new ChannelApiService();
