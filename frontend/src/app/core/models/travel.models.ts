export interface ApiList<T> {
  items?: T[];
  data?: T[];
  total?: number;
}

export interface Itinerary {
  id: string;
  title: string;
  destination: string;
  startDate: string;
  endDate: string;
  latitude?: number;
  longitude?: number;
  budget?: number;
  notes?: string;
  stops?: ItineraryStop[];
}

export interface ItineraryStop {
  id: string;
  name: string;
  address?: string;
  latitude: number;
  longitude: number;
  dayNumber?: number;
  orderIndex?: number;
  visitDate?: string;
  notes?: string;
  category?: number;
  durationMinutes?: number;
}

export interface Hotel {
  id: string;
  name: string;
  city: string;
  country?: string;
  pricePerNight: number;
  rating?: number;
  imageUrl?: string;
  amenities?: string[];
}

export interface Booking {
  id: string;
  hotelId?: string;
  flightId?: string;
  itineraryId?: string;
  status: string;
  totalPrice: number;
  createdAt?: string;
}

export interface Flight {
  id: string;
  airline: string;
  origin: string;
  destination: string;
  departureAt: string;
  returnAt?: string;
  price: number;
  currency?: string;
}

export interface FlightAlert {
  id: string;
  origin: string;
  destination: string;
  targetPrice: number;
  enabled: boolean;
}

export interface ChatMessage {
  id?: string;
  itineraryId: string;
  role: 'user' | 'assistant';
  content: string;
  createdAt?: string;
}

export interface ReportSummary {
  itineraryId: string;
  totalBudget?: number;
  totalSpent: number;
  balance?: number;
  expenses: Expense[];
}

export interface Expense {
  id: string;
  category: string;
  description: string;
  amount: number;
  date: string;
}

export interface AdminUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  isActive: boolean;
}
