import { AbstractApiService } from './AbstractApiService';

class VersionApiService extends AbstractApiService {
    public constructor() {
        super('/api/version');
    }

    public version(): Promise<string> {
        return this.http
            .get('')
            .then(this.handleResponse)
            .catch(this.handleError.bind(this));
    }
}

export const versionApiService: VersionApiService = new VersionApiService();
