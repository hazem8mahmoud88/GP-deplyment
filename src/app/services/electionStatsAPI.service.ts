import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";


@Injectable({
    providedIn: 'root'
})
export class ElectionStatsAPI {
    baseUrl: string = 'https://localhost:7087/api'
    httpClient: HttpClient = inject(HttpClient);

    countVotes(electionID: number) {
        return this.httpClient.post(`${this.baseUrl}/results/count/${electionID}`, {})
    }

    getElectionStats(electionID: number) {
        return this.httpClient.get(`${this.baseUrl}/results/${electionID}`)
    }

    getElectionStatsByGov(electionID: number) {
        return this.httpClient.get(`${this.baseUrl}/results/${electionID}/by-governorate`)
    }

    getParticipationRate(electionID: number) {
        return this.httpClient.get(`${this.baseUrl}/results/${electionID}/participation`)
    }

    getElectionStatsByConstituency(electionID: number, governorateID: number) {
        return this.httpClient.get(`${this.baseUrl}/results/${electionID}/by-constituency/${governorateID}`)
    }

    getDemographicStats(electionID: number) {
        return this.httpClient.get(`${this.baseUrl}/results/${electionID}/demographics`)
    }
}
