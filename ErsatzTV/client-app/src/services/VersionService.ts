import { AbstractApiService } from './AbstractApiService';

class VersionApiService extends AbstractApiService {
    public constructor() {
        super();
    }

    public version(): Promise<string> {
        return this.http
            .get('/api/version')
            .then(this.handleResponse.bind(this))
            .catch(this.handleError.bind(this));
    }
}

export const versionApiService: VersionApiService = new VersionApiService();
