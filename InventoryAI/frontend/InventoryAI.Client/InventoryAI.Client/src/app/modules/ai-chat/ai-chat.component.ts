// ai-chat.component.ts
import { Component, OnInit, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AiChatService } from '../../core/services/services';

@Component({
  standalone: true,
  selector: 'app-ai-chat',
  imports: [CommonModule, FormsModule],
  templateUrl: './ai-chat.component.html'
})
export class AiChatComponent implements OnInit, AfterViewChecked {
  @ViewChild('chatBody') chatBody!: ElementRef;

  sessionId: string | null = null;
  messages: { role: string; content: string; time: Date }[] = [];
  userInput = '';
  isLoading = false;
  isStarting = true;

  quickQuestions = [
    'Which products are running low on stock?',
    'What are the top selling products?',
    'How many products are out of stock?',
    'Give me a summary of current inventory health',
    'Which products should I reorder urgently?'
  ];

  constructor(private chatService: AiChatService) {}

  ngOnInit() {
    this.startSession();
  }

  ngAfterViewChecked() {
    this.scrollToBottom();
  }

  startSession() {
    this.isStarting = true;
    this.chatService.startSession().subscribe({
      next: res => {
        this.sessionId  = res.sessionId;
        this.isStarting = false;
        this.addMessage('assistant',
          '👋 Hello! I\'m your AI Inventory Assistant powered by **RAG** (Retrieval-Augmented Generation).\n\n' +
          'I have real-time access to your inventory data. You can ask me about:\n' +
          '• Stock levels and low-stock alerts\n• Product details and pricing\n' +
          '• Sales trends and recommendations\n• Reorder suggestions\n\n' +
          'How can I help you today?'
        );
      },
      error: () => {
        this.isStarting = false;
        this.addMessage('assistant', '⚠️ Could not start AI session. Please ensure the backend is running.');
      }
    });
  }

  sendMessage(text?: string) {
    const message = text || this.userInput.trim();
    if (!message || !this.sessionId || this.isLoading) return;

    this.addMessage('user', message);
    this.userInput = '';
    this.isLoading = true;

    this.chatService.sendMessage(message, this.sessionId).subscribe({
      next: res => {
        this.addMessage('assistant', res.response);
        this.isLoading = false;
      },
      error: () => {
        this.addMessage('assistant', '❌ Error connecting to AI service. Please check that Ollama is running.');
        this.isLoading = false;
      }
    });
  }

  addMessage(role: string, content: string) {
    this.messages.push({ role, content, time: new Date() });
  }

  onKeyDown(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  scrollToBottom() {
    try {
      this.chatBody.nativeElement.scrollTop = this.chatBody.nativeElement.scrollHeight;
    } catch {}
  }

  clearChat() {
    this.messages = [];
    this.startSession();
  }
}
