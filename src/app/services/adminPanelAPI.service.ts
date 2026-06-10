import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from "@angular/core";
import { environment } from "../../environments/environment";

@Injectable({
  providedIn: 'root'
})
export class AdminPanelAPI {
  baseUrl: string = `${environment.apiUrl}/api`
  httpClient: HttpClient = inject(HttpClient)

  addOrganizer(id: number, organizerID: number) {
    let organizerInfo: Object = {
      organizerId: organizerID,
      canDecrypt: true
    }

    return this.httpClient.post(`${this.baseUrl}/elections/${id}/organizers`, organizerInfo)
  }

  getOrganizers(id: number) {
    return this.httpClient.get(`${this.baseUrl}/elections/${id}/organizers`)
  }

  getAllOrganizers() {
    return this.httpClient.get(`${this.baseUrl}/admin/organizers`);
  }

  deleteOrganizer(id: number, organizerID: number) {
    return this.httpClient.delete(`${this.baseUrl}/elections/${id}/organizers/${organizerID}`)
  }
}
