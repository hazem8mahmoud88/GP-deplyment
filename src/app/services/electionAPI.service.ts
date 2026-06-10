import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Election } from "../models/election.model";


@Injectable({
  providedIn: 'root'
})
export class ElectionApi {
  http: HttpClient = inject(HttpClient);
  url: string = 'https://localhost:7087/api/elections';

  createElection(data: Election) {
    return this.http.post(this.url, data);
  }

  getElections() {
    return this.http.get(this.url)
  }

  getElectionById(id: number) {
    return this.http.get(`${this.url}/${id}`)
  }

  updateElection(id: number, data: Election) {
    return this.http.put(`${this.url}/${id}`, data);
  }

  deleteElection(electionID: string) {
    return this.http.delete(`https://localhost:7087/api/Elections/${electionID}`);
  }

  activateElection(electionID: string) {
    return this.http.put(`https://localhost:7087/api/Elections/${electionID}/activate`, {})
  }

  closeElection(electionID: string) {
    return this.http.put(`${this.url}/${electionID}/close`, {})
  }

}
